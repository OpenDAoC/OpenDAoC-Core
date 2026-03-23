using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public abstract class ServiceObjectArrayBase<T> : IServiceObjectArray
        where T : class, IServiceObject
    {
        // Indicates how the caller should iterate over this collection.
        public abstract bool IsSharded { get; }

        // Flat array API.
        public abstract List<T> Items { get; }
        public abstract int LastValidIndex { get; }

        // Sharded array API.
        public abstract List<T>[] Shards { get; }
        public abstract int[] ShardStartIndices { get; }
        public abstract int TotalValidCount { get; }

        public abstract void Add(T item);
        public abstract void Schedule(T item, long wakeUpTimeMs);
        public abstract void Remove(T item);
        public abstract void Update(long now);
    }

    public readonly struct ServiceObjectView<T> where T : class, IServiceObject
    {
        public readonly bool IsSharded;
        public readonly int TotalValidCount;
        public readonly List<T> Items;
        public readonly List<T>[] Shards;
        public readonly int[] ShardStartIndices;

        public ServiceObjectView(List<T> items, int totalValidCount)
        {
            IsSharded = false;
            Items = items;
            TotalValidCount = totalValidCount;
            Shards = null;
            ShardStartIndices = null;
        }

        public ServiceObjectView(List<T>[] shards, int[] shardStartIndices, int totalValidCount)
        {
            IsSharded = true;
            Shards = shards;
            ShardStartIndices = shardStartIndices;
            TotalValidCount = totalValidCount;
            Items = null;
        }

        public void ExecuteForEach(Action<T> action)
        {
            if (TotalValidCount <= 0)
                return;

            if (IsSharded)
                GameLoop.ExecuteForEachSharded(Shards, ShardStartIndices, TotalValidCount, action);
            else
                GameLoop.ExecuteForEach(Items, TotalValidCount, action);
        }
    }
}
