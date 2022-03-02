using DOL.AI.Brain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS.Debug;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Buffers;

namespace DOL.GS
{
    public class TaskStats
    {
        public long CreationTime;
        public int Name;
        public int ThreadNum;
        public int Completed;
        public int Unthinking;
    }

    public static class NPCThinkService
    {
        static int _segmentsize = 5000;
        static List<Task> _tasks = new List<Task>();
        static int completed;
        static int unthinking;
        static long interval = 2000;
        static long last_interval = 0;

        private const string ServiceName = "NPCThinkService";

        static NPCThinkService()
        {
            EntityManager.AddService(typeof(NPCThinkService));
        }

        public static void Tick(long tick)
        {

            Diagnostics.StartPerfCounter(ServiceName);

            GameLiving[] arr = EntityManager.GetAllNpcsArrayRef();

            Parallel.ForEach(arr, npc =>
            {
                if (npc == null)
                {
                    return;
                }
                if (npc is GameNPC && (npc as GameNPC).Brain != null)
                {
                    var brain = (npc as GameNPC).Brain;

                    if (brain.IsActive && brain.LastThinkTick + brain.ThinkInterval < tick)
                    {
                        brain.Think();
                        brain.LastThinkTick = tick;
                    }

                }
            });
            
            Diagnostics.StopPerfCounter(ServiceName);
        }

    }
}
