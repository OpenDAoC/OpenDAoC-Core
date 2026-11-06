using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ShardedServiceObjectArray<T> : ServiceObjectArrayBase<T>
        where T : class, IShardedServiceObject
    {
        private readonly List<SchedulableServiceObjectArray<T>> _shards;
        private readonly List<T>[] _shardsView;
        private readonly int[] _shardStartIndices;
        private readonly Action<SchedulableServiceObjectArray<T>> _updateShardView;
        private int _roundRobinCounter;
        private int _totalValidCount;

        public override List<T> Items => null;
        public override int LastValidIndex => TotalValidCount - 1;
        public override List<T>[] Shards => _shardsView;
        public override int[] ShardStartIndices => _shardStartIndices;
        public override int TotalValidCount => _totalValidCount;

        public ShardedServiceObjectArray(int initialCapacity)
        {
            int shardCount = GameLoop.DegreeOfParallelism;
            _shards = new(shardCount);
            _shardsView = new List<T>[shardCount];

            int capacityPerShard = initialCapacity / shardCount;

            for (int i = 0; i < shardCount; i++)
            {
                _shards.Add(new(capacityPerShard));
                _shardsView[i] = _shards[i].Items;
            }

            _shardStartIndices = new int[shardCount];
            _updateShardView = shard => shard.UpdateAndGetView(_currentUpdateNow);
        }

        private ServiceObjectArrayBase<T> RouteItem(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (id.ShardIndex == -1)
                id.ShardIndex = (int) ((uint) Interlocked.Increment(ref _roundRobinCounter) % _shards.Count);

            return _shards[id.ShardIndex];
        }

        protected override void Add(T item)
        {
            Add(RouteItem(item), item);
        }

        protected override void Remove(T item)
        {
            Remove(RouteItem(item), item);
        }

        protected override bool TryPrepareAdd(T item)
        {
            return TryPrepareAdd(RouteItem(item), item);
        }

        protected override bool TryPrepareRemove(T item)
        {
            return item.ServiceObjectId.ShardIndex != -1 && TryPrepareRemove(RouteItem(item), item);
        }

        protected override ServiceObjectView<T> UpdateAndGetView()
        {
            GameLoop.ExecuteForEach(_shards, _shards.Count, _updateShardView);

            int currentTotal = 0;

            for (int i = 0; i < Shards.Length; i++)
            {
                _shardStartIndices[i] = currentTotal;
                currentTotal += _shards[i].LastValidIndex + 1;
            }

            _totalValidCount = currentTotal;
            return new(_shardsView, _shardStartIndices, _totalValidCount);
        }
    }
}
