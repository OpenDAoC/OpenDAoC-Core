using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // Manages a contiguous array of active items.
    public class ServiceObjectArray<T> : ServiceObjectArrayBase<T>
        where T : class, IServiceObject
    {
        private readonly DrainArray<T> _itemsToAdd = new();
        private readonly DrainArray<T> _itemsToRemove = new();
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

        protected override void Add(T item)
        {
            _itemsToAdd.Add(item);
        }

        protected override void Remove(T item)
        {
            _itemsToRemove.Add(item);
        }

        protected override bool TryPrepareAdd(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            // Prevent re-entry if the object is already registered, but not being removed.
            if (id.IsRegistered && id.PeekAction() is not ServiceObjectId.PendingAction.Remove)
                return false;

            // Prevent re-entry if the object is already being added.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Add))
                return false;

            return true;
        }

        protected override bool TryPrepareRemove(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

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
            if (_itemsToRemove.Any)
                _itemsToRemove.DrainTo(static (item, ctx) => ctx.RemoveInternal(item), this);
        }

        protected virtual void ProcessItemsToAdd()
        {
            if (_itemsToAdd.Any)
                _itemsToAdd.DrainTo(static (item, ctx) => ctx.AddInternal(item), this);
        }

        protected static bool TryPrepareRemoveFromList(ServiceObjectId id)
        {
            return id.TryConsumeAction(ServiceObjectId.PendingAction.Remove);
        }

        protected virtual void RemoveInternal(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (!TryPrepareRemoveFromList(id))
                return;

            RemoveFromList(id);
            id.Unset();
        }

        protected static bool TryPrepareAddToList(ServiceObjectId id)
        {
            return id.TryConsumeAction(ServiceObjectId.PendingAction.Add);
        }

        protected virtual void AddInternal(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (!TryPrepareAddToList(id))
                return;

            AddToList(item);
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

        protected void AddToList(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (id.IsRunning)
                return;

            if (++_lastValidIndex >= Items.Capacity)
            {
                int newCapacity = (int) (Items.Capacity * 1.2);

                if (Items.Capacity == newCapacity)
                    newCapacity++;

                Items.Resize(newCapacity);
            }

            if (LastValidIndex >= Items.Count)
                Items.Add(item);
            else
                Items[LastValidIndex] = item;

            id.Value = LastValidIndex;
        }
    }
}
