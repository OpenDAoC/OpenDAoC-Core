using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS
{
    public static class GameLoop
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const bool DYNAMIC_BUSY_WAIT_THRESHOLD = true; // Setting it to false disables busy waiting completely unless a default value is given to '_busyWaitThreshold'.
        private const string THREAD_NAME = "GameLoop";

        private static Thread _gameLoopThread;
        private static Thread _busyWaitThresholdThread;
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
                    Name = "BusyWaitThreshold",
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
        }

        public static List<(int, double)> GetAverageTps()
        {
            return _gameLoopStats.GetAverageTicks(GameLoopTime);
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
                    log.Info($"Thread \"{_gameLoopThread.Name}\" was interrupted");
                    return;
                }
                catch (Exception e)
                {
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
                EffectListService.Tick();
                ZoneService.Tick();
                CraftingService.Tick();
                ReaperService.Tick();
                ClientService.EndTick();
                DailyQuestService.Tick();
                WeeklyQuestService.Tick();
                ConquestService.Tick();
                BountyService.Tick();
                PredatorService.Tick();
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

                if (busyWaitThreshold > 0 && TickRate > stopwatch.Elapsed.TotalMilliseconds)
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
            Stopwatch stopwatch = new();
            stopwatch.Start();

            try
            {
                while (true)
                {
                    double start;
                    double overSleptFor;
                    double highest = 0;

                    for (int i = 0; i < 20; i++)
                    {
                        start = stopwatch.Elapsed.TotalMilliseconds;
                        Thread.Sleep(1);
                        overSleptFor = stopwatch.Elapsed.TotalMilliseconds - start - 1;

                        if (highest < overSleptFor)
                            highest = overSleptFor;
                    }

                    _busyWaitThreshold = Math.Max(0, (int) highest);
                    Thread.Sleep(20000);
                }
            }
            catch (ThreadInterruptedException)
            {
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
                _intervals = intervals;
                _intervals.OrderByDescending(x => x);
            }

            public void RecordTick(double gameLoopTime)
            {
                // Clean up outdated timestamps to prevent the queue from growing indefinitely.
                while (_tickTimestamps.TryPeek(out double _oldestTickTimestamp) && gameLoopTime - _oldestTickTimestamp > _intervals[0])
                    _tickTimestamps.TryDequeue(out _);

                _tickTimestamps.Enqueue(gameLoopTime);
            }

            public List<(int, double)> GetAverageTicks(long currentTime)
            {
                List<(int, double)> averages = new(); // Result.
                Dictionary<int, double> tickCounts = new();

                foreach (int interval in _intervals)
                    tickCounts[interval] = 0;

                List<double> tickTimestampsSnapshot = _tickTimestamps.ToList(); // Copy to allow thread safety.
                int startIndex = 0;

                // Count ticks per interval and calculate averages.
                foreach (int interval in _intervals)
                {
                    double timestampToUse = 0;

                    for (int i = startIndex; i < tickTimestampsSnapshot.Count; i++)
                    {
                        double timestamp = tickTimestampsSnapshot[i];
                        double age = currentTime - timestamp;

                        if (age > interval)
                        {
                            timestampToUse = timestamp;
                            continue;
                        }

                        tickCounts[interval] += tickTimestampsSnapshot.Count - i;
                        startIndex = i;
                        break;
                    }

                    if (timestampToUse == 0)
                    {
                        if (tickTimestampsSnapshot.Count == 0)
                        {
                            averages.Add((interval, 0));
                            continue;
                        }

                        timestampToUse = tickTimestampsSnapshot[0];
                    }

                    double tickCount = tickCounts[interval];
                    double actualInterval = currentTime - timestampToUse;
                    averages.Add((interval, actualInterval == 0 ? 0 : tickCount / actualInterval * 1000));
                }

                return averages;
            }
        }
    }
}
