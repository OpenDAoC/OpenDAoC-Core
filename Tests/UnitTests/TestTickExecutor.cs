using System;
using System.Collections.Generic;

namespace DOL.GS.Tests
{
    internal sealed class TestExecutor : IExecutor
    {
        public static readonly TestExecutor Instance = new(new TestSerialTickExecutor());

        private readonly ITickExecutor _tickExecutor;

        public int DegreeOfParallelism => Environment.ProcessorCount;
        public ITickExecutor TickExecutor => _tickExecutor;

        public TestExecutor(ITickExecutor tickExecutor)
        {
            _tickExecutor = tickExecutor ?? throw new ArgumentNullException(nameof(tickExecutor));
        }

        public void ExecuteView<T>(in ServiceObjectView<T> view, Action<T> action) where T : class, IServiceObject
        {
            _tickExecutor.ExecuteView(view, action);
        }

        public void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            _tickExecutor.ExecuteForEach(items, toExclusive, action);
        }

        public void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action)
        {
            _tickExecutor.ExecuteForEachSharded(shards, shardStartIndices, totalCount, action);
        }
    }

    internal sealed class TestSerialTickExecutor : ITickExecutor
    {
        public void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            for (int i = 0; i < toExclusive; i++)
                action(items[i]);
        }

        public void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action)
        {
            if (totalCount <= 0)
                return;

            for (int i = 0; i < shards.Length; i++)
            {
                int shardValidCount;

                if (i < shards.Length - 1)
                    shardValidCount = shardStartIndices[i + 1] - shardStartIndices[i];
                else
                    shardValidCount = totalCount - shardStartIndices[i];

                if (shardValidCount <= 0)
                    continue;

                List<T> currentShard = shards[i];

                for (int j = 0; j < shardValidCount; j++)
                    action(currentShard[j]);
            }
        }
    }
}
