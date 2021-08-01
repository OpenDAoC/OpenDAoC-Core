using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public static class GameLoop
    {
        public static long GameLoopTime=0;
        
        //GameLoop tick timer -> will adjust based on the performance
        private static long _tickDueTime = 50;
        private static Timer _timerRef;
        
        //Max player count is 4000
        public static GamePlayer[] players = new GamePlayer[4000];
        private static int _lastPlayerIndex = 0;

        
        public static bool Init()
        {
            _timerRef = new Timer(Tick,null,0,Timeout.Infinite);
            return true;
        }


        private static void Tick(object obj)
        {
            //Make sure the tick < gameLoopTick
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();


            CastingService.Tick(GameLoopTime);
            EffectService.Tick(GameLoopTime);

            GameLoopTime += _tickDueTime;

            stopwatch.Stop();
            var elapsed = (float) stopwatch.Elapsed.TotalMilliseconds;

            
            //We need to delay our next threading time to the default tick time. If this is > 0, we delay the next tick until its met to maintain consistent tick rate
            var diff = (int) (50 - elapsed);
            if (diff <= 0)
            {
                Console.WriteLine($"Tick rate unable to keep up with load! Elapsed: {elapsed}");
                _timerRef.Change(0, Timeout.Infinite);
                return;
            }

            //Console.WriteLine($"Elapsed: {elapsed}");
            _timerRef.Change(diff, Timeout.Infinite);


        }
    }
}