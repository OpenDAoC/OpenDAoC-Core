using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class CastingService
    {
        private const string ServiceName = "CastingService";
        static int _segmentsize = 2;
        static List<Task> _tasks = new List<Task>();
        static CastingService()
        {
            EntityManager.AddService(typeof(CastingService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);
            GameLiving[] arr = EntityManager.GetLivingByComponent(typeof(CastingComponent));

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
                            HandleTick(livings[index], tick);
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
        private static void HandleTick(GameLiving p,long tick)
        {
            if (p == null)
                return;

            if (p.castingComponent?.instantSpellHandler != null)
                p.castingComponent.instantSpellHandler.Tick(tick);

            if (p.castingComponent?.spellHandler == null)
                return;

            var handler = p.castingComponent.spellHandler;
                
            handler.Tick(tick);
        }
    }
}