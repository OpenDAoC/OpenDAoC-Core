using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using log4net;

namespace DOL.GS
{
    public static class GameLoop
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const long TICK_RATE = 50;
        private const int BUSY_WAIT_THRESHOLD = 10; // Set to 0 to disable busy waiting.
        private const string THREAD_NAME = "GameLoop";

        public static long GameLoopTime;
        public static string CurrentServiceTick;
        private static Thread _gameLoopThread;
        private static Stopwatch _stopwatch = new();
        private static long _stopwatchFrequencyMilliseconds = Stopwatch.Frequency / 1000;
        private static int _sleepFor;

        // Previously in 'GameTimer'. Not sure where this should be moved to.
        public static long GetCurrentTime()
        {
            return Stopwatch.GetTimestamp() / _stopwatchFrequencyMilliseconds;
        }

        public static bool Init()
        {
            _gameLoopThread = new Thread(new ThreadStart(Run))
            {
                Priority = ThreadPriority.AboveNormal,
                Name = THREAD_NAME,
                IsBackground = true
            };
            _gameLoopThread.Start();
            return true;
        }

        public static void Exit()
        {
            if (_gameLoopThread == null)
                return;

            _gameLoopThread.Interrupt();
            _gameLoopThread = null;
        }

        private static void Run()
        {
            _stopwatch.Start();

            while (true)
            {
                try
                {
                    TickServices();
                    Sleep();
                }
                catch (ThreadInterruptedException)
                {
                    log.Info($"Game loop was interrupted");
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
                CraftingService.Tick(GameLoopTime);
                TimerService.Tick(GameLoopTime);
                AuxTimerService.Tick(GameLoopTime);
                ReaperService.Tick();
                ClientService.Tick();
                DailyQuestService.Tick();
                WeeklyQuestService.Tick();
                ConquestService.Tick();
                BountyService.Tick(GameLoopTime);
                PredatorService.Tick(GameLoopTime);
                ECS.Debug.Diagnostics.Tick();
                CurrentServiceTick = "";
                ECS.Debug.Diagnostics.StopPerfCounter(THREAD_NAME);
            }

            static void Sleep()
            {
                _sleepFor = (int) (TICK_RATE - _stopwatch.Elapsed.TotalMilliseconds);

                if (_sleepFor >= BUSY_WAIT_THRESHOLD)
                {
                    Thread.Sleep(_sleepFor - BUSY_WAIT_THRESHOLD);

                    while (ShouldBusyWait())
                        Thread.Yield();
                }
                else
                {
                    do
                    {
                        Thread.Yield();
                    } while (ShouldBusyWait());
                }

                GameLoopTime += (long) _stopwatch.Elapsed.TotalMilliseconds;
                _stopwatch.Restart();

                static bool ShouldBusyWait()
                {
                    return TICK_RATE >= _stopwatch.Elapsed.TotalMilliseconds;
                }
            }
        }
    }
}
