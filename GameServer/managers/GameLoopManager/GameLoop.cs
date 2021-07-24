using System;
using System.Threading;

namespace GameServer.managers.GameLoopManager
{
    public static class GameLoop
    {
        public static long GameLoopTime=0;
        
        //GameLoop tick timer -> will adjust based on the performance
        private static readonly long _interval = 25;
        
        public static bool Init()
        {
            var gameLoop = new Timer(Tick,null,0,_interval);
            return true;
        }


        private static void Tick(object obj)
        {
            Console.WriteLine($"Tick {GameLoopTime}");
            GameLoopTime += _interval;
        }
    }
}