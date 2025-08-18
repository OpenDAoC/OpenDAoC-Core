using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public sealed class GameLoopThreadPoolSingleThreaded : GameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public override void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            CheckResetTick();

            for (int i = 0; i < toExclusive; i++)
                action(items[i]);
        }

        public override void Dispose() { }
    }
}
