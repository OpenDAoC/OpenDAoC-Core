using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static GameLoopThreadPool _threadPool;
        private static int _busyWaitThreshold;
        private static long _stopwatchFrequencyMilliseconds = Stopwatch.Frequency / 1000;
        private static GameLoopStats _gameLoopStats;
        private static bool _running;

        public static long TickRate { get; private set; }
        public static long PreviousGameLoopTime { get; private set; }
        public static long GameLoopTime { get; private set; }
        public static string CurrentServiceTick { get; set; }

        // This is unrelated to the game loop and should probably be moved elsewhere.
        public static long GetCurrentTime()
        {
            return Stopwatch.GetTimestamp() / _stopwatchFrequencyMilliseconds;
        }

        public static bool Init()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
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
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (Thread.CurrentThread != _gameLoopThread && _gameLoopThread.IsAlive)
                _gameLoopThread.Join();

            if (_busyWaitThresholdThread.IsAlive)
            {
                _busyWaitThresholdThread.Interrupt(); // This thread sleeps for a long time.
                _busyWaitThresholdThread.Join();
            }

            _threadPool.Dispose();
        }

        public static List<(int, double)> GetAverageTps()
        {
            return _gameLoopStats.GetAverageTicks(GameLoopTime);
        }

        public static void ExecuteWork(int count, Action<int> action)
        {
            _threadPool.ExecuteWork(count, action);
        }

        public static T GetForTick<T>(PooledObjectKey poolKey, Action<T> initializer) where T : IPooledObject<T>, new()
        {
            return _threadPool.GetForTick(poolKey, initializer);
        }

        private static void Run()
        {
            if (Environment.ProcessorCount == 1)
                _threadPool = new GameLoopThreadPoolSingleThreaded();
            else
                _threadPool = new GameLoopThreadPoolMultiThreaded(Environment.ProcessorCount);

            _threadPool.Init(); // Must be done from the game loop thread.

            double gameLoopTime = 0;
            double elapsedTime = 0;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            while (Volatile.Read(ref _running))
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
                        log.Info($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");

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

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");

            static void TickServices()
            {
                ECS.Debug.Diagnostics.StartPerfCounter(THREAD_NAME);

                GameLoopService.BeginTick();
                TimerService.Tick();
                ClientService.BeginTick();
                NpcService.Tick();
                AttackService.Tick();
                CastingService.Tick();
                ZoneService.Tick();
                CraftingService.Tick();
                ReaperService.Tick();
                ClientService.EndTick();
                DailyQuestService.Tick();
                WeeklyQuestService.Tick();
                GameLoopService.EndTick();

                _threadPool.PrepareForNextTick();

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
                PreviousGameLoopTime = GameLoopTime;
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
                while (Volatile.Read(ref _running))
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
                    log.Info($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");

                return;
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }
    }
}
