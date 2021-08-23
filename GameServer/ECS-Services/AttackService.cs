using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class AttackService
    {
        private const string ServiceName = "AttackService";

        static AttackService()
        {
            EntityManager.AddService(typeof(AttackService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            foreach (var living in EntityManager.GetLivingByComponent(typeof(AttackComponent)))
            {
                if (living == null)
                    continue;

                if (living.attackComponent is null)
                    continue;

                living.attackComponent.Tick(tick);
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {

        }
    }
}
