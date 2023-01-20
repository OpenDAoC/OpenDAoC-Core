using System;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class CastingService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string ServiceName = "CastingService";

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
                    if (p?.castingComponent == null)
                        return;

                    long startTick = GameTimer.GetTickCount();
                    p.castingComponent.Tick(tick);
                    long stopTick = GameTimer.GetTickCount();

                    if ((stopTick - startTick) > 25)
                        log.Warn($"Long CastingComponent.Tick for: {p.Name}({p.ObjectID}) Spell: {p.castingComponent?.spellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in CastingService: {e}");
                }
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }
    }
}
