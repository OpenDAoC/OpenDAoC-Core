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
            TotalValidCount = totalValidCount;
            Items = items;
            Shards = null;
            ShardStartIndices = null;
        }

        public ServiceObjectView(List<T>[] shards, int[] shardStartIndices, int totalValidCount)
        {
            IsSharded = true;
            TotalValidCount = totalValidCount;
            Items = null;
            Shards = shards;
            ShardStartIndices = shardStartIndices;
        }
    }
}
