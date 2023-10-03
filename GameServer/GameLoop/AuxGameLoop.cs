using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using log4net;

namespace DOL.GS
{
    // AuxGameLoop's purpose is to run services that can run concurrently with GameLoop.
    public static class AuxGameLoop
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const long TICK_RATE = 50;
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
            _gameLoopThread?.Interrupt();
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

            try
            {
                ClientService.Tick();
                AuxTimerService.Tick(GameLoopTime);
            }
            catch (Exception e)
            {
                log.Error($"Critical error encountered in {nameof(AuxGameLoop)}: {e}");
                GameServer.Instance.Stop();
                return;
            }

            // ECS.Debug.Diagnostics.Tick();
            // ECS.Debug.Diagnostics.StopPerfCounter(THREAD_NAME);

            GameLoopTime = GameLoop.GetCurrentTime();
            _stopwatch.Stop();

            float elapsed = (float) _stopwatch.Elapsed.TotalMilliseconds;

            // We need to delay our next threading time to the default tick time. If this is > 0, we delay the next tick until its met to maintain consistent tick rate.
            int diff = (int) (TICK_RATE - elapsed);

            if (diff <= 0)
            {
                _timerRef.Change(0, Timeout.Infinite);
                return;
            }

            _timerRef.Change(diff, Timeout.Infinite);
        }
    }
}
