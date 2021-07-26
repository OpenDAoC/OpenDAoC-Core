using System;
using System.Threading;

namespace DOL.GS
{
    public static class GameLoop
    {
        public static long GameLoopTime=0;
        
        //GameLoop tick timer -> will adjust based on the performance
        private static readonly long _interval = 25;
        private static Timer _timerRef;
        
        //Max player count is 4000
        public static GamePlayer[] players = new GamePlayer[4000];
        private static int _lastPlayerIndex = 0;
        
        public static bool Init()
        {
            _timerRef = new Timer(Tick,null,0,_interval);
            return true;
        }


        private static void Tick(object obj)
        {
            
            //Because I'm lazy
            var clients = WorldMgr.GetAllClients();
            foreach (var client in clients)
            {
                
                var p = client?.Player;
                if (p == null)
                {
                    continue;
                }
                if (p.castingComponent?.spellHandler != null)
                {
                    p.castingComponent.Tick(GameLoopTime);
                }
                if (p.attackComponent?.attackAction != null)
                {
                    p.attackComponent.Tick(GameLoopTime);   
                } 
            }
            
            
            //Make sure the tick < gameLoopTick
            //Timer.stopwatch()
            for (int i = _lastPlayerIndex; i < players.Length; i++)
            {
                GamePlayer p = players[i];
                
                // //Check for spell
                // if (p.SpellComponent?.SpellHandler != null)
                // {
                //     p.SpellComponent.Tick();
                // }
            }
            //Console.WriteLine($"Tick {GameLoopTime}");
            GameLoopTime += _interval;
            
            //Timer.stopwatch() <-- check time, if time > _interval, interval goes up 5ms
        }
    }
}