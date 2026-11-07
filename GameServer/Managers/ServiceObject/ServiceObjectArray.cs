using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // Manages a contiguous array of active items.
    public class ServiceObjectArray<T, TId> : ServiceObjectArrayBase<T, TId>
        where T : class, IServiceObject<T>
        where TId : ServiceObjectId<T>
    {
        private readonly DrainArray<TId> _idsToAdd = new();
        private readonly DrainArray<TId> _idsToRemove = new();
        private bool _updating;
        private int _lastValidIndex = -1;

        public override List<T> Items { get; }
        public override int LastValidIndex => _lastValidIndex;
        public override List<T>[] Shards => null;
        public override int[] ShardStartIndices => null;
        public override int TotalValidCount => LastValidIndex + 1;

        public ServiceObjectArray(int capacity)
        {
            Items = new(capacity);
        }

        public override bool Add(TId id)
        {
            if (!TryPrepareAdd(id))
                return false;

            _idsToAdd.Add(id);
            return true;
        }

        public override bool Remove(TId id)
        {
            if (!TryPrepareRemove(id))
                return false;

            _idsToRemove.Add(id);
            return true;
        }

        public override bool Reschedule(TId id)
        {
            return false;
        }

        protected virtual bool TryPrepareAdd(TId id)
        {
            // Prevent re-entry if the object is already registered, but not being removed.
            if (id.IsRegistered && id.PeekAction() is not ServiceObjectId.PendingAction.Remove)
                return false;

            // Prevent re-entry if the object is already being added.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Add))
                return false;

            return true;
        }

        protected virtual bool TryPrepareRemove(TId id)
        {
            // Prevent re-entry if the object is already unregistered, but not being added.
            if (!id.IsRegistered && id.PeekAction() is not ServiceObjectId.PendingAction.Add)
                return false;

            // Prevent re-entry if the object is already being removed.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Remove))
                return false;

            return true;
        }

        protected override ServiceObjectView<T> UpdateAndGetView()
        {
            if (Interlocked.Exchange(ref _updating, true) != false)
                throw new InvalidOperationException($"{typeof(T)} is already being updated.");

            try
            {
                ProcessItemsToRemove();
                ProcessItemsToAdd();
            }
            finally
            {
                _updating = false;
            }

            return new(Items, LastValidIndex + 1);
        }

        protected virtual void ProcessItemsToRemove()
        {
            if (_idsToRemove.Any)
                _idsToRemove.DrainTo(static (id, ctx) => ctx.RemoveInternal(id), this);
        }

        protected virtual void ProcessItemsToAdd()
        {
            if (_idsToAdd.Any)
                _idsToAdd.DrainTo(static (id, ctx) => ctx.AddInternal(id), this);
        }

        protected static bool TryPrepareRemoveFromList(ServiceObjectId id)
        {
            return id.TryConsumeAction(ServiceObjectId.PendingAction.Remove);
        }

        protected virtual void RemoveInternal(TId id)
        {
            if (!TryPrepareRemoveFromList(id))
                return;

            RemoveFromList(id);
            id.Unset();
        }

        protected static bool TryPrepareAddToList(ServiceObjectId id)
        {
            return id.TryConsumeAction(ServiceObjectId.PendingAction.Add);
        }

        protected virtual void AddInternal(TId id)
        {
            if (!TryPrepareAddToList(id))
                return;

            AddToList(id);
        }

        protected void RemoveFromList(ServiceObjectId id)
        {
            int idValue = id.Value;

            if (idValue < 0 || idValue > LastValidIndex)
                return;

            // Swap the item being removed with the item at the end.
            if (idValue == LastValidIndex)
                Items[LastValidIndex] = null;
            else
            {
                T lastItem = Items[LastValidIndex];
                Items[idValue] = lastItem;
                lastItem.ServiceObjectId.Value = idValue;
                Items[LastValidIndex] = null;
            }

            _lastValidIndex--;
        }

        protected void AddToList(TId id)
        {
            if (id.IsRunning)
                return;

            if (++_lastValidIndex >= Items.Capacity)
            {
                int newCapacity = (int) (Items.Capacity * 1.2);

                if (Items.Capacity == newCapacity)
                    newCapacity++;

                Items.Resize(newCapacity);
            }

            T item = id.Owner;

            if (LastValidIndex >= Items.Count)
                Items.Add(item);
            else
                Items[LastValidIndex] = item;

            id.Value = LastValidIndex;
        }
    }

    public class ServiceObjectArray<T> : ServiceObjectArray<T, ServiceObjectId<T>>
        where T : class, IServiceObject<T>
    {
        public ServiceObjectArray(int capacity) : base(capacity) { }
    }
}
