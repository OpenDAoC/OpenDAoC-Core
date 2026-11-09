using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ShardedServiceObjectArray<T> : ServiceObjectArrayBase<T, ShardedServiceObjectId<T>>
        where T : class, IShardedServiceObject<T>
    {
        private readonly IExecutor _executor;
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

        public ShardedServiceObjectArray(int initialCapacity, IExecutor executor)
        {
            _executor = executor;

            int shardCount = _executor.DegreeOfParallelism;
            int capacityPerShard = initialCapacity / shardCount;

            _shards = new(shardCount);
            _shardsView = new List<T>[shardCount];

            for (int i = 0; i < shardCount; i++)
            {
                _shards.Add(new(capacityPerShard));
                _shardsView[i] = _shards[i].Items;
            }

            _shardStartIndices = new int[shardCount];
            _updateShardView = shard => shard.UpdateAndGetView(_currentUpdateNow);
        }

        private SchedulableServiceObjectArray<T> RouteItem(ShardedServiceObjectId<T> id)
        {
            if (id.ShardIndex == -1)
                id.ShardIndex = (int) ((uint) Interlocked.Increment(ref _roundRobinCounter) % _shards.Count);

            return _shards[id.ShardIndex];
        }

        public override bool Add(ShardedServiceObjectId<T> id)
        {
            return RouteItem(id).Add(id);
        }

        public override bool Remove(ShardedServiceObjectId<T> id)
        {
            return id.ShardIndex != -1 && _shards[id.ShardIndex].Remove(id);
        }

        public override bool Reschedule(ShardedServiceObjectId<T> id)
        {
            return id.ShardIndex != -1 && _shards[id.ShardIndex].Reschedule(id);
        }

        protected override ServiceObjectView<T> UpdateAndGetView()
        {
            _executor.ExecuteForEach(_shards, _shards.Count, _updateShardView);

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
