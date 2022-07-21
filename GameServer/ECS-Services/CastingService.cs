using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;
using System.Reflection;

namespace DOL.GS
{
    public static class CastingService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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
                try
                {
                    long startTick = GameTimer.GetTickCount();
                    HandleTick(p, tick);
                    long stopTick = GameTimer.GetTickCount();
                    if ((stopTick - startTick) > 25)
                        log.Warn(
                            $"Long CastingComponent.Tick for {p.Name}({p.ObjectID}) Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in CastingService: {e}");
                }
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(GameLiving p,long tick)
        {
            if (p == null)
                return;

            if (p.castingComponent?.spellHandler == null)
                return;

            var handler = p.castingComponent.spellHandler;

            handler?.Tick(tick);
        }
    }
}