using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class CastingService
    {
        private const string ServiceName = "CastingService";
        static int _segmentsize = 2;
        static List<Task> _tasks = new List<Task>();
        static CastingService()
        {
            EntityManager.AddService(typeof(CastingService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);
            GameLiving[] arr = EntityManager.GetLivingByComponent(typeof(CastingComponent));

            
            Parallel.ForEach(arr, p =>
            {
                HandleTick(p, tick);
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(GameLiving p,long tick)
        {
            if (p == null)
                return;

            if (p.castingComponent?.instantSpellHandler != null)
                p.castingComponent.instantSpellHandler.Tick(tick);

            if (p.castingComponent?.spellHandler == null)
                return;

            var handler = p.castingComponent.spellHandler;
                
            handler.Tick(tick);
        }
    }
}