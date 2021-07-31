using System;

namespace DOL.GS
{
    public static class CastingService
    {
        public static void Tick(long tick)
        {
            
            foreach (var p in EntityManager.GetAllPlayers())
            {
                if (p == null)
                    continue;
                
                if(p.castingComponent?.spellHandler == null)
                    continue;


                var handler = p.castingComponent.spellHandler;
                
                handler.Tick(tick);
            }
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {
            
        }
    }
}