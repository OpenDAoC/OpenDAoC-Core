using DOL.AI.Brain;
using ECS.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public static class NPCThinkService
    {
        private const string ServiceName = "NPCThinkService";

        static NPCThinkService()
        {
            EntityManager.AddService(typeof(NPCThinkService));
        }
        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            foreach (var npc in EntityManager.GetAllNpcs())
            {
                if (npc == null)
                    continue;

                if(npc is GameNPC && (npc as GameNPC).Brain != null)
                {
                    var brain = (npc as GameNPC).Brain;

                    if (brain.IsActive && brain.LastThinkTick + brain.ThinkInterval < tick)
                    {
                        brain.Think();
                        brain.LastThinkTick = tick;
                    }
                }


                
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {

        }
    }
}
