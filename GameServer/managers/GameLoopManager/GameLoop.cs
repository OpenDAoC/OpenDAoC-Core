using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public static class GameLoop
    {
        public static long GameLoopTime=0;
        
        //GameLoop tick timer -> will adjust based on the performance
        private static long _interval = 25;
        private static Timer _timerRef;
        
        //Max player count is 4000
        public static GamePlayer[] players = new GamePlayer[4000];
        private static int _lastPlayerIndex = 0;

        // private static CastingService _castingService;
        
        public static bool Init()
        {
            _timerRef = new Timer(Tick,null,0,_interval);
            return true;
        }


        private static void Tick(object obj)
        {
            //Make sure the tick < gameLoopTick
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();


            CastingService.Tick(GameLoopTime);

            GameLoopTime += _interval;

            //check time, if time > _interval, interval goes up 5ms
            stopwatch.Stop();
            if(stopwatch.ElapsedMilliseconds > _interval)
            {
                _interval += 5;
                _timerRef.Change(0, _interval);
                Console.WriteLine("Increasing interval by 5ms. New interval: " + _interval);
            }
        }
    }
}