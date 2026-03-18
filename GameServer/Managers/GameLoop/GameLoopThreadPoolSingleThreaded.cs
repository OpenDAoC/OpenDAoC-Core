using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public sealed class GameLoopThreadPoolSingleThreaded : GameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public override void ExecuteForEach<T>(IReadOnlyList<T> items, int toExclusive, Action<T> action)
        {
            CheckResetTick();

            for (int i = 0; i < toExclusive; i++)
                action(items[i]);
        }

        public override void ExecuteForEachSharded<T>(IReadOnlyList<IReadOnlyList<T>> shards, int[] shardStartIndices, int totalCount, Action<T> action)
        {
            CheckResetTick();

            if (totalCount <= 0)
                return;

            for (int i = 0; i < shards.Count; i++)
            {
                int shardValidCount;

                if (i < shards.Count - 1)
                    shardValidCount = shardStartIndices[i + 1] - shardStartIndices[i];
                else
                    shardValidCount = totalCount - shardStartIndices[i];

                if (shardValidCount <= 0)
                    continue;

                IReadOnlyList<T> currentShard = shards[i];

                for (int j = 0; j < shardValidCount; j++)
                    action(currentShard[j]);
            }
        }

        public override void Dispose() { }
    }
}
