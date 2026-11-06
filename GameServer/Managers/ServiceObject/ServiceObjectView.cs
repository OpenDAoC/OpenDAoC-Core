using System;
using System.Collections.Generic;

namespace DOL.GS
{
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
