using System.Collections.Generic;

namespace DOL.GS
{
    public abstract class ServiceObjectArrayBase<T>
        where T : class, IServiceObject<T>
    {
        protected long _currentUpdateNow;

        // Flat array API.
        public abstract List<T> Items { get; }
        public abstract int LastValidIndex { get; }

        // Sharded array API.
        public abstract List<T>[] Shards { get; }
        public abstract int[] ShardStartIndices { get; }
        public abstract int TotalValidCount { get; }

        public abstract void Publish();
        protected abstract ServiceObjectView<T> UpdateAndGetView();

        public ServiceObjectView<T> UpdateAndGetView(long now)
        {
            _currentUpdateNow = now;
            return UpdateAndGetView();
        }
    }

    public abstract class ServiceObjectArrayBase<T, TId> : ServiceObjectArrayBase<T>
        where T : class, IServiceObject<T>
        where TId : ServiceObjectId<T>
    {
        public abstract bool Add(TId id);
        public abstract bool Remove(TId id);
        public abstract bool Reschedule(TId id);

        public override void Publish()
        {
            ServiceObjectStore.Publish(this);
        }
    }
}
