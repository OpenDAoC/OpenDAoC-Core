using System;

namespace DOL.GS
{
    public class SchedulableServiceObjectArray<T> : ServiceObjectArray<T, SchedulableServiceObjectId<T>>
        where T : class, ISchedulableServiceObject<T>
    {
        // Circular timing wheel with a priority queue for distant scheduled items.

        // Scheduling uses floor division, so items may wake up early.
        // Actual wake-up time also depends on the game loop's update frequency.
        private const int WHEEL_RESOLUTION_MS = 30;

        // 1 << 16 = 65,536 buckets.
        // 65,536 buckets * 30ms = 3,276,800ms = ~32.8 minutes.
        private const int WHEEL_BITS = 16;
        private const int WHEEL_SIZE = 1 << WHEEL_BITS;
        private const int WHEEL_MASK = WHEEL_SIZE - 1;

        private readonly SchedulableServiceObjectId<T>[] _heads = new SchedulableServiceObjectId<T>[WHEEL_SIZE];
        private readonly DrainArray<SchedulableServiceObjectId<T>> _idsToReschedule = new();
        private long _lastProcessedWheelTick = -1;

        public SchedulableServiceObjectArray(int capacity) : base(capacity) { }

        private void Link(SchedulableServiceObjectId<T> id, int bucket)
        {
            SchedulableServiceObjectId<T> head = _heads[bucket];
            id.SleepPrev = null;
            id.SleepNext = head;
            head?.SleepPrev = id;
            _heads[bucket] = id;
            id.SleepBucket = bucket;
        }

        private void Unlink(SchedulableServiceObjectId<T> id)
        {
            SchedulableServiceObjectId<T> prev = id.SleepPrev;
            SchedulableServiceObjectId<T> next = id.SleepNext;

            if (prev != null)
                prev.SleepNext = next;
            else
                _heads[id.SleepBucket] = next;

            next?.SleepPrev = prev;
            id.SleepPrev = id.SleepNext = null;
            id.SleepBucket = -1;
        }

        private void WakeUpItems()
        {
            long currentUpdateTick = _currentUpdateNow / WHEEL_RESOLUTION_MS;

            if (_lastProcessedWheelTick == -1)
                _lastProcessedWheelTick = currentUpdateTick;

            // Catch up on all missed ticks.
            while (_lastProcessedWheelTick <= currentUpdateTick)
            {
                int bucket = (int) (_lastProcessedWheelTick & WHEEL_MASK);

                SchedulableServiceObjectId<T> id = _heads[bucket];
                _heads[bucket] = null;

                while (id != null)
                {
                    SchedulableServiceObjectId<T> next = id.SleepNext;
                    id.SleepNext = id.SleepPrev = null;
                    id.SleepBucket = -1;

                    if (id.PeekAction() is ServiceObjectId.PendingAction.None)
                    {
                        long target = id.NextTickMs / WHEEL_RESOLUTION_MS;

                        if (target > _lastProcessedWheelTick)
                            Link(id, GetBucket(target));
                        else
                            AddToList(id);
                    }

                    id = next;
                }

                _lastProcessedWheelTick++;
            }
        }

        public override bool Reschedule(SchedulableServiceObjectId<T> id)
        {
            // A pending addition already reads the latest wake time, and a pending removal takes precedence.
            if (!id.IsRegistered || id.PeekAction() is ServiceObjectId.PendingAction.Add or ServiceObjectId.PendingAction.Remove)
                return false;

            long target = id.NextTickMs / WHEEL_RESOLUTION_MS;
            long currentTick = _currentUpdateNow / WHEEL_RESOLUTION_MS;

            if (id.IsRunning)
            {
                // In the active list and still due at the next update.
                if (target <= currentTick)
                    return false;
            }
            else if (target > currentTick && id.SleepBucket == GetBucket(target))
            {
                // Sleeping in the right bucket already.
                return false;
            }

            // Returns false if the id is already queued for a move.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Move))
                return false;

            _idsToReschedule.Add(id);
            return true;
        }

        protected override void ProcessItemsToAdd()
        {
            ProcessItemsToReschedule();
            WakeUpItems();
            base.ProcessItemsToAdd();
        }

        private void ProcessItemsToReschedule()
        {
            if (!_idsToReschedule.Any)
                return;

            if (_lastProcessedWheelTick == -1)
                _lastProcessedWheelTick = _currentUpdateNow / WHEEL_RESOLUTION_MS;

            _idsToReschedule.DrainTo(static (id, ctx) => ctx.RescheduleInternal(id), this);
        }

        private void RescheduleInternal(SchedulableServiceObjectId<T> id)
        {
            // The id may have been removed, or queued for removal, since Reschedule was called.
            if (!id.TryConsumeAction(ServiceObjectId.PendingAction.Move))
                return;

            Schedule(id);
        }

        protected override void RemoveInternal(SchedulableServiceObjectId<T> id)
        {
            if (!TryPrepareRemoveFromList(id))
                return;

            if (id.SleepBucket >= 0)
                Unlink(id);

            RemoveFromList(id);
            id.Unset();
        }

        protected override void AddInternal(SchedulableServiceObjectId<T> id)
        {
            if (!TryPrepareAddToList(id))
                return;

            Schedule(id);
        }

        private void Schedule(SchedulableServiceObjectId<T> id)
        {
            if (id.SleepBucket >= 0)
                Unlink(id);

            long wakeUp = id.NextTickMs;

            if (wakeUp - _currentUpdateNow <= 0)
            {
                AddToList(id);
                return;
            }

            long targetTick = wakeUp / WHEEL_RESOLUTION_MS;

            if (targetTick < _lastProcessedWheelTick)
            {
                AddToList(id);
                return;
            }

            RemoveFromList(id);
            id.Value = ServiceObjectId.SLEEPING_ID;
            Link(id, GetBucket(targetTick));
        }

        private int GetBucket(long targetTick)
        {
            return (int) (Math.Min(targetTick, _lastProcessedWheelTick + WHEEL_SIZE - 1) & WHEEL_MASK);
        }
    }
}
