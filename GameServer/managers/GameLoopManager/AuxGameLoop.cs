using System.Diagnostics;
using System.Threading;

namespace DOL.GS
{
    // The AuxGameLoop is for Timers that do not need to be "realtime" and part of the main Game Loop. 
    // This is for things like quit timers, player init, world init, etc where we dont really care how long it takes as long as it doesnt affect the main Game Loop.
    public static class AuxGameLoop
    {
        private const long TICK_RATE = 50; // GameLoop tick timer. Will adjust based on the performance.
        private const string THREAD_NAME = "AuxGameLoop";
        public static long GameLoopTime;
        private static Thread _gameLoopThread;
        private static Timer _timerRef;
        private static Stopwatch _stopwatch = new();

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
            // ECS.Debug.Diagnostics.StartPerfCounter(THREAD_NAME);

            AuxTimerService.Tick(GameLoopTime);

            // ECS.Debug.Diagnostics.Tick();
            // ECS.Debug.Diagnostics.StopPerfCounter(THREAD_NAME);

            GameLoopTime = GameTimer.GetTickCount();
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
