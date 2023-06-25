using System.Diagnostics;
using System.Threading;
using DOL.GS.Scripts;

namespace DOL.GS
{
    public static class GameLoop
    {
        public const long TICK_RATE = 50; // GameLoop tick timer. Will adjust based on the performance.
        private const string THREAD_NAME = "GameLoop";
        public static long GameLoopTime;
        public static string CurrentServiceTick;
        private static Thread _gameLoopThread;
        private static Timer _timerRef;
        private static Stopwatch _stopwatch = new();
        private static long _stopwatchFrequencyMilliseconds = Stopwatch.Frequency / 1000;

        // Previously in 'GameTimer'. Not sure where this should be moved to.
        public static long GetCurrentTime()
        {
            return Stopwatch.GetTimestamp() / _stopwatchFrequencyMilliseconds;
        }

        public static bool Init()
        {
            _gameLoopThread = new Thread(new ThreadStart(GameLoopThreadStart))
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
            _gameLoopThread.Interrupt();
            _gameLoopThread = null;
        }

        private static void GameLoopThreadStart()
        {
            _timerRef = new Timer(Tick, null, 0, Timeout.Infinite);
        }

        private static void Tick(object obj)
        {
            _stopwatch.Restart();
            ECS.Debug.Diagnostics.StartPerfCounter(THREAD_NAME);

            NpcService.Tick(GameLoopTime);
            AttackService.Tick(GameLoopTime);
            CastingService.Tick(GameLoopTime);
            EffectService.Tick();
            EffectListService.Tick(GameLoopTime);
            ZoneService.Tick();
            CraftingService.Tick(GameLoopTime);
            TimerService.Tick(GameLoopTime);
            ReaperService.Tick();
            DailyQuestService.Tick();
            WeeklyQuestService.Tick();
            ConquestService.Tick();
            BountyService.Tick(GameLoopTime);
            PredatorService.Tick(GameLoopTime);

            if (ZoneBonusRotator._lastPvEChangeTick == 0)
                ZoneBonusRotator._lastPvEChangeTick = GameLoopTime;
            if (ZoneBonusRotator._lastRvRChangeTick == 0)
                ZoneBonusRotator._lastRvRChangeTick = GameLoopTime;

            ECS.Debug.Diagnostics.Tick();
            CurrentServiceTick = "";
            ECS.Debug.Diagnostics.StopPerfCounter(THREAD_NAME);
            GameLoopTime = GetCurrentTime();
            _stopwatch.Stop();

            float elapsed = (float)_stopwatch.Elapsed.TotalMilliseconds;
            // We need to delay our next threading time to the default tick time. If this is > 0, we delay the next tick until its met to maintain consistent tick rate.
            int diff = (int)(TICK_RATE - elapsed);

            if (diff <= 0)
            {
                _timerRef.Change(0, Timeout.Infinite);
                return;
            }

            _timerRef.Change(diff, Timeout.Infinite);
        }
    }
}
