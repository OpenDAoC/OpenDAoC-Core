using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public sealed class GameLoopExecutor : IExecutor
    {
        public static readonly GameLoopExecutor Instance = new();

        public int DegreeOfParallelism => GameLoop.DegreeOfParallelism;

        public void ExecuteView<T>(in ServiceObjectView<T> view, Action<T> action) where T : class, IServiceObject
        {
            GameLoop.ExecuteView(view, action);
        }

        public void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            GameLoop.ExecuteForEach(items, toExclusive, action);
        }

        public void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action)
        {
            GameLoop.ExecuteForEachSharded(shards, shardStartIndices, totalCount, action);
        }
    }

    public interface IExecutor
    {
        int DegreeOfParallelism { get; }
        void ExecuteView<T>(in ServiceObjectView<T> view, Action<T> action) where T : class, IServiceObject;
        void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action);
        void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action);
    }
}
