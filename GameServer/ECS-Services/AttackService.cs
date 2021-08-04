using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public static class AttackService
    {
        public static void Tick(long tick)
        {

            foreach (var p in EntityManager.GetAllPlayers())
            {
                if (p == null)
                    continue;

                if (p.attackComponent is null)
                    continue;

                p.attackComponent.Tick(tick);
            }
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {

        }
    }
}
