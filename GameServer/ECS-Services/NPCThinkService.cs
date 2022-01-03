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

            lock (arr)
            {
                for (int ctr = 1; ctr <= Math.Ceiling(((double)arr.Count()) / _segmentsize); ctr++)
                {
                    int elements = _segmentsize;
                    int offset = (ctr - 1) * _segmentsize;
                    int upper = offset + elements;
                    if ((upper) > arr.Count())
                        elements = arr.Count() - offset;

                    ArraySegment<GameLiving> segment = new ArraySegment<GameLiving>(arr, offset, elements);

                    _tasks.Add(Task.Factory.StartNew((Object obj) =>
                    {
                        TaskStats data = obj as TaskStats;
                        if (data == null)
                            return;

                        data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                        IList<GameLiving> npc = (IList<GameLiving>)segment;

                        for (int index = 0; index < npc.Count; index++)
                        {
                            if (npc[index] == null)
                                continue;

                            if (npc[index] is GameNPC && (npc[index] as GameNPC).Brain != null)
                            {
                                var brain = (npc[index] as GameNPC).Brain;

                                if (brain.IsActive && brain.LastThinkTick + brain.ThinkInterval < tick)
                                {
                                    brain.Think();
                                    brain.LastThinkTick = tick;
                                    data.Completed += 1;
                                }
                                else
                                {
                                    data.Unthinking += 1;
                                }
                            }
                        }
                        data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                    },
                    new TaskStats() { Name = ctr, CreationTime = DateTime.Now.Ticks }));
                }
                Task.WaitAll(_tasks.ToArray());
            }

            /*
            foreach (var task in _tasks)
            {
                var data = task.AsyncState as TaskStats;
                if (data != null)
                {
                    completed += data.Completed;
                    unthinking += data.Unthinking;
                }
            }
            */

            _tasks.Clear();

            /*
            if (last_interval + interval < tick)
            {
                Console.WriteLine("{0} thoughts completed this period. {1} unthinking scanned.", completed, unthinking);
                Console.WriteLine("Threads: {0}", Process.GetCurrentProcess().Threads.Count);
                completed = 0;
                unthinking = 0;
                last_interval = tick;
            }
            */

            Diagnostics.StopPerfCounter(ServiceName);
        }

        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {

        }
    }
}
