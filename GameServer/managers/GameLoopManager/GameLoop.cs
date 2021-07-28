using System;
using System.Threading;

namespace DOL.GS
{
    public static class GameLoop
    {
        public static long GameLoopTime=0;
        
        //GameLoop tick timer -> will adjust based on the performance
        private static long _interval = 10;
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
            //Make sure the tick < gameLoopTick
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();


            //Change this to a list of all entities
            //currently does not handle any non-player entities
            var clients = WorldMgr.GetAllClients();
            foreach (var client in clients)
            {
                
                var p = client?.Player;
                if (p == null)
                {
                    continue;
                }

                //to conform to ECS move this to a new class -> CastingSystem
                if (p.castingComponent?.spellHandler != null)
                {
                    p.castingComponent.Tick(GameLoopTime);
                }

                //here we would call a new class -> MeleeSystem

                //here we would call a new class -> DamageSystem

                //here we would call a new class -> MovementSystem
            }


            
            /*
            for (int i = _lastPlayerIndex; i < players.Length; i++)
            {
                GamePlayer p = players[i];
                
                // //Check for spell
                // if (p.SpellComponent?.SpellHandler != null)
                // {
                //     p.SpellComponent.Tick();
                // }
            }
            */
            //Console.WriteLine($"Tick {GameLoopTime}");
            GameLoopTime += _interval;

            //check time, if time > _interval, interval goes up 5ms
            stopwatch.Stop();
            if(stopwatch.ElapsedMilliseconds > _interval)
            {
                _interval += 5;
                Console.WriteLine("Increasing interval by 5ms. New interval: " + _interval);
            }
        }
    }
}