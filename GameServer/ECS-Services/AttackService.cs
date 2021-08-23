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

            foreach (var p in EntityManager.GetAllPlayers())
            {
                if (p == null)
                    continue;

                if (p.attackComponent is null)
                    continue;

                p.attackComponent.Tick(tick);
            }
            foreach (var npc in EntityManager.GetAllNpcs())
            {
                if (npc == null)
                    continue;

                if (npc.attackComponent is null)
                    continue;

                npc.attackComponent.Tick(tick);
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {

        }
    }
}
