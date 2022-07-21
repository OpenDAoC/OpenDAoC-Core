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
using log4net;
using System.Reflection;

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

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
                try
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
                            long startTick = GameTimer.GetTickCount();
                            brain.Think();
                            long stopTick = GameTimer.GetTickCount();
                            if((stopTick - startTick)  > 25 && brain != null)
                                log.Warn($"Long NPCThink for {brain.Body?.Name}({brain.Body?.ObjectID}) BrainType: {brain.GetType().ToString()} Time: {stopTick - startTick}ms");
                            brain.LastThinkTick = tick;
                        }

                        if (brain.Body is not {NeedsBroadcastUpdate: true}) return;
                        brain.Body.BroadcastUpdate();
                        brain.Body.NeedsBroadcastUpdate = false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Critical error encountered in NPC Think: {e}");
                }
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }

    }
}
