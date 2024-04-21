using System;
using System.Diagnostics;
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

            _gameLoopThread.Interrupt();
            _gameLoopThread.Join();
            _gameLoopThread = null;
            _busyWaitThresholdThread.Interrupt();
            _busyWaitThresholdThread.Join();
            _busyWaitThresholdThread = null;
        }

        private static void Run()
        {
            double gameLoopTime = 0;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            while (true)
            {
                try
                {
                    TickServices();
                    Sleep(stopwatch);
                    gameLoopTime += stopwatch.Elapsed.TotalMilliseconds;
                    GameLoopTime = (long) Math.Round(gameLoopTime);
                    stopwatch.Restart();
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
                NpcService.Tick();
                AttackService.Tick();
                CastingService.Tick();
                EffectService.Tick();
                EffectListService.Tick();
                ZoneService.Tick();
                CraftingService.Tick();
                TimerService.Tick();
                ReaperService.Tick();
                ClientService.Tick();
                DailyQuestService.Tick();
                WeeklyQuestService.Tick();
                ConquestService.Tick();
                BountyService.Tick();
                PredatorService.Tick();
                ECS.Debug.Diagnostics.Tick();
                CurrentServiceTick = string.Empty;
                ECS.Debug.Diagnostics.StopPerfCounter(THREAD_NAME);
            }

            static void Sleep(Stopwatch stopwatch)
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
    }
}
