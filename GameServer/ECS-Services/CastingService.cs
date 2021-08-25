using System;
using ECS.Debug;

namespace DOL.GS
{
    public static class CastingService
    {
        private const string ServiceName = "CastingService";
        static CastingService()
        {
            EntityManager.AddService(typeof(CastingService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            foreach (var p in EntityManager.GetAllPlayers())
            {
                if (p == null)
                    continue;
                
                if(p.castingComponent?.spellHandler == null)
                    continue;


                var handler = p.castingComponent.spellHandler;
                
                handler.Tick(tick);
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {
            
        }
    }
}