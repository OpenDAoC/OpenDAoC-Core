using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public static class GameLoop
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public const string THREAD_NAME = "GameLoop";
        private const bool DYNAMIC_BUSY_WAIT_THRESHOLD = true; // Setting it to false disables busy waiting completely unless a default value is given to '_busyWaitThreshold'.

        private static Thread _gameLoopThread; // Main thread.
        private static Thread _busyWaitThresholdThread; // Secondary thread that attempts to calculate by how much `Thread.Sleep` overshoots.
        private static IGameLoopThreadPool _workerThreadPool;
        private static int _busyWaitThreshold;
        private static long _stopwatchFrequencyMilliseconds = Stopwatch.Frequency / 1000;
        private static GameLoopStats _gameLoopStats;

        public static long TickRate { get; private set; }
        public static long GameLoopTime { get; private set; }
        public static string CurrentServiceTick { get; set; }

        // This is unrelated to the game loop and should probably be moved elsewhere.
        public static long GetCurrentTime()
        {
            return Stopwatch.GetTimestamp() / _stopwatchFrequencyMilliseconds;
        }

        public static bool Init()
        {
            if (_gameLoopThread != null)
                return false;

            TickRate = Properties.GAME_LOOP_TICK_RATE;
            _gameLoopStats = new([60000, 30000, 10000]);

            if (Environment.ProcessorCount == 1)
                _workerThreadPool = new GameLoopThreadPoolSingleThreaded();
            else
                _workerThreadPool = new GameLoopThreadPool(Environment.ProcessorCount);

            _gameLoopThread = new Thread(new ThreadStart(Run))
            {
                Name = THREAD_NAME,
                IsBackground = true
            };
            _gameLoopThread.Start();

            if (DYNAMIC_BUSY_WAIT_THRESHOLD)
            {
                _busyWaitThresholdThread = new Thread(new ThreadStart(UpdateBusyWaitThreshold))
                {
                    Name = $"{THREAD_NAME}_BusyWaitThreshold",
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = true
                };
                _busyWaitThresholdThread.Start();
            }

            return true;
        }

        public static void Exit()
        {
            if (_gameLoopThread == null)
                return;

            if (Thread.CurrentThread != _gameLoopThread)
            {
                _gameLoopThread.Interrupt();
                _gameLoopThread.Join();
            }

            _gameLoopThread = null;
            _busyWaitThresholdThread.Interrupt();
            _busyWaitThresholdThread.Join();
            _busyWaitThresholdThread = null;
            _workerThreadPool.Dispose();
            _workerThreadPool = null;
        }

        public static List<(int, double)> GetAverageTps()
        {
            return _gameLoopStats.GetAverageTicks(GameLoopTime);
        }

        public static void Work(int count, Action<int> action)
        {
            _workerThreadPool.Work(count, action);
        }

        private static void Run()
        {
            double gameLoopTime = 0;
            double elapsedTime = 0;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            while (true)
            {
                try
                {
                    TickServices();
                    Sleep();
                    elapsedTime = stopwatch.Elapsed.TotalMilliseconds;
                    stopwatch.Restart();
                    UpdateStatsAndTime(elapsedTime);
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Thread \"{_gameLoopThread.Name}\" was interrupted");

                    return;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Critical error encountered in {nameof(GameLoop)}: {e}");

                    GameServer.Instance.Stop();
                    return;
                }
            }

            static void TickServices()
            {
                ECS.Debug.Diagnostics.StartPerfCounter(THREAD_NAME);
                TimerService.Tick();
                ClientService.BeginTick();
                NpcService.Tick();
                AttackService.Tick();
                CastingService.Tick();
                EffectService.Tick();
                ZoneService.Tick();
                CraftingService.Tick();
                ReaperService.Tick();
                ClientService.EndTick();
                DailyQuestService.Tick();
                WeeklyQuestService.Tick();
                ECS.Debug.Diagnostics.Tick();
                CurrentServiceTick = string.Empty;
                ECS.Debug.Diagnostics.StopPerfCounter(THREAD_NAME);
            }

            void Sleep()
            {
                int sleepFor = (int) (TickRate - stopwatch.Elapsed.TotalMilliseconds);
                int busyWaitThreshold = _busyWaitThreshold;

                if (sleepFor >= busyWaitThreshold)
                    Thread.Sleep(sleepFor - busyWaitThreshold);
                else
                    Thread.Yield();

                if (TickRate > stopwatch.Elapsed.TotalMilliseconds)
                {
                    SpinWait spinWait = new();

                    while (TickRate > stopwatch.Elapsed.TotalMilliseconds)
                        spinWait.SpinOnce(-1);
                }
            }

            void UpdateStatsAndTime(double elapsed)
            {
                gameLoopTime += elapsed;
                GameLoopTime = (long) Math.Round(gameLoopTime);
                _gameLoopStats.RecordTick(gameLoopTime);
            }
        }

        private static void UpdateBusyWaitThreshold()
        {
            int maxIteration = 10;
            int sleepFor = 1;
            int pauseFor = 10000;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            try
            {
                while (true)
                {
                    double start;
                    double overSleptFor;
                    double highest = 0;

                    for (int i = 0; i < maxIteration; i++)
                    {
                        start = stopwatch.Elapsed.TotalMilliseconds;
                        Thread.Sleep(sleepFor);
                        overSleptFor = stopwatch.Elapsed.TotalMilliseconds - start - sleepFor;

                        if (highest < overSleptFor)
                            highest = overSleptFor;
                    }

                    _busyWaitThreshold = Math.Max(0, (int) highest);
                    Thread.Sleep(pauseFor);
                }
            }
            catch (ThreadInterruptedException)
            {
                if (log.IsInfoEnabled)
                    log.Info($"Thread \"{_busyWaitThresholdThread.Name}\" was interrupted");

                return;
            }
        }

        private class GameLoopStats
        {
            private ConcurrentQueue<double> _tickTimestamps = new();
            private List<int> _intervals;

            public GameLoopStats(List<int> intervals)
            {
                // Intervals to use for average ticks per second. Must be in descending order.
                _intervals = intervals.OrderByDescending(x => x).ToList();
            }

            public void RecordTick(double gameLoopTime)
            {
                double oldestAllowed = gameLoopTime - _intervals[0];

                // Clean up outdated timestamps to prevent the queue from growing indefinitely.
                while (_tickTimestamps.TryPeek(out double _oldestTickTimestamp) && _oldestTickTimestamp < oldestAllowed)
                    _tickTimestamps.TryDequeue(out _);

                _tickTimestamps.Enqueue(gameLoopTime);
            }

            public List<(int, double)> GetAverageTicks(long currentTime)
            {
                List<(int, double)> averages = new();
                List<double> snapshot = _tickTimestamps.ToList(); // Copy for thread safety.
                int startIndex = 0;

                // Count ticks per interval and calculate averages.
                foreach (int interval in _intervals)
                {
                    double intervalStart = currentTime - interval;
                    int tickCount = 0;

                    // Find the number of ticks within this interval.
                    for (int i = startIndex; i < snapshot.Count; i++)
                    {
                        if (snapshot[i] >= intervalStart)
                        {
                            tickCount = snapshot.Count - i;
                            startIndex = i;
                            break;
                        }
                    }

                    double actualInterval;

                    if (tickCount > 0)
                        actualInterval = snapshot[^1] - snapshot[startIndex];
                    else
                        actualInterval = interval;

                    double average = (tickCount - 1) / (actualInterval / 1000.0);
                    averages.Add((interval, average));
                }

                return averages;
            }
        }
    }
}
