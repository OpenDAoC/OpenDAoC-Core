using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class AttackService
    {
        static int _segmentsize = 1000;
        static List<Task> _tasks = new List<Task>();

        private const string ServiceName = "AttackService";

        static AttackService()
        {
            EntityManager.AddService(typeof(AttackService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            GameLiving[] arr = EntityManager.GetLivingByComponent(typeof(AttackComponent));

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
                        IList<GameLiving> livings = (IList<GameLiving>)segment;

                        for (int index = 0; index < livings.Count; index++)
                        {
                            if (livings[index] == null)
                                continue;

                            if (livings[index].attackComponent is null)
                                continue;

                            livings[index].attackComponent.Tick(tick);
                        }
                        data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                    },
                    new TaskStats() { Name = ctr, CreationTime = DateTime.Now.Ticks }));
                }
                Task.WaitAll(_tasks.ToArray());
            }


            _tasks.Clear();

            Diagnostics.StopPerfCounter(ServiceName);
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {

        }
    }
}
