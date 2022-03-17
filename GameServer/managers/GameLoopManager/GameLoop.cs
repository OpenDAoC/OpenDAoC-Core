using DOL.GS.Scripts;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public static class GameLoop
    {
        public static long GameLoopTime=0;
        private const string PerfCounterName = "GameLoop";
        private static Thread m_GameLoopThread;

        //GameLoop tick timer -> will adjust based on the performance
        private static long _tickDueTime = 50;
        private static Timer _timerRef;
        
        //Max player count is 4000
        public static GamePlayer[] players = new GamePlayer[4000];
        private static int _lastPlayerIndex = 0;

        public static long TickRate { get { return _tickDueTime; } }

        public static bool Init()
        {
            m_GameLoopThread = new Thread(new ThreadStart(GameLoopThreadStart));
            m_GameLoopThread.Priority = ThreadPriority.AboveNormal;
            m_GameLoopThread.Name = "GameLoop";
            m_GameLoopThread.IsBackground = true;
            m_GameLoopThread.Start();
            
            return true;
        }

        public static void Exit()
        {
            m_GameLoopThread.Interrupt();
            m_GameLoopThread = null;
        }

        private static void GameLoopThreadStart()
        {
            bool running = true;
            _timerRef = new Timer(Tick, null, 0, Timeout.Infinite);

            while (running)
            {
                try { }
                catch (ThreadInterruptedException)
                {
                    running = false;
                }
            }
        }

        private static void Tick(object obj)
        {
            ECS.Debug.Diagnostics.StartPerfCounter(PerfCounterName);
            
            //Make sure the tick < gameLoopTick
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            NPCThinkService.Tick(GameLoopTime);
            AttackService.Tick(GameLoopTime);
            CastingService.Tick(GameLoopTime);
            EffectService.Tick(GameLoopTime);
            EffectListService.Tick(GameLoopTime);
            DailyQuestService.Tick(GameLoopTime);

            if (ZoneBonusRotator._lastPvEChangeTick == 0)
                ZoneBonusRotator._lastPvEChangeTick = GameLoopTime;
            if (ZoneBonusRotator._lastRvRChangeTick == 0)
                ZoneBonusRotator._lastRvRChangeTick = GameLoopTime;

            //Always tick last!
            ECS.Debug.Diagnostics.Tick();

            ECS.Debug.Diagnostics.StopPerfCounter(PerfCounterName);

            GameLoopTime = GameTimer.GetTickCount();

            stopwatch.Stop();
            var elapsed = (float)stopwatch.Elapsed.TotalMilliseconds;

            //We need to delay our next threading time to the default tick time. If this is > 0, we delay the next tick until its met to maintain consistent tick rate
            var diff = (int) (_tickDueTime - elapsed);
            if (diff <= 0)
            {
                //Console.WriteLine($"Tick rate unable to keep up with load! Elapsed: {elapsed}");
                _timerRef.Change(0, Timeout.Infinite);
                return;
            }

            _timerRef.Change(diff, Timeout.Infinite);
        }
    }
}