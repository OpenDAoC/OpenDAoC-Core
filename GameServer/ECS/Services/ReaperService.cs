using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public class ReaperService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ReaperService);

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<LivingBeingKilled> list = EntityManager.UpdateAndGetAll<LivingBeingKilled>(EntityManager.EntityType.LivingBeingKilled, out int lastValidIndex);

            // Remove objects from one sub zone, and add them to another.
            Parallel.For(0, lastValidIndex + 1, i =>
            {
                LivingBeingKilled livingBeingKilled = list[i];

                if (livingBeingKilled?.EntityManagerId.IsSet != true)
                    return;

                try
                {
                    livingBeingKilled.Killed.ProcessDeath(livingBeingKilled.Killer);
                    EntityManager.Remove(livingBeingKilled);
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, livingBeingKilled, livingBeingKilled.Killed);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static void KillLiving(GameLiving killed, GameObject killer)
        {
            LivingBeingKilled.Create(killed, killer);
        }
    }

    // Temporary objects to be added to 'EntityManager' and consumed by 'ReaperService', representing a living object being killed and waiting to be processed.
    public class LivingBeingKilled : IManagedEntity
    {
        public GameLiving Killed { get; private set; }
        public GameObject Killer { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.LivingBeingKilled, true);

        private LivingBeingKilled(GameLiving killed, GameObject killer)
        {
            Initialize(killed, killer);
        }

        public static void Create(GameLiving killed, GameObject killer)
        {
            if (EntityManager.TryReuse(EntityManager.EntityType.LivingBeingKilled, out LivingBeingKilled livingBeingKilled))
                livingBeingKilled.Initialize(killed, killer);
            else
            {
                livingBeingKilled = new(killed, killer);
                EntityManager.Add(livingBeingKilled);
            }
        }

        private void Initialize(GameLiving killed, GameObject killer)
        {
            Killed = killed;
            Killer = killer;
        }
    }
}
