using System.Threading;

namespace DOL.GS
{
    public class ServiceObjectId
    {
        public const int UNSET_ID = -1;

        private int _action = (int) PendingAction.None;
        private int _value = UNSET_ID;

        public int Value => Volatile.Read(ref _value);
        public ServiceObjectType Type { get; }

        public bool IsRegistered => Value != UNSET_ID;
        public bool IsRunning => Value >= 0;
        public bool IsDormant => IsRegistered && !IsRunning;

        public ServiceObjectId(ServiceObjectType type)
        {
            Type = type;
        }

        public bool TrySetAction(PendingAction action)
        {
            return Interlocked.Exchange(ref _action, (int) action) != (int) action;
        }

        public bool TryConsumeAction(PendingAction expectedAction)
        {
            return Interlocked.CompareExchange(ref _action, (int) PendingAction.None, (int) expectedAction) == (int) expectedAction;
        }

        public PendingAction PeekAction()
        {
            return (PendingAction) Volatile.Read(ref _action);
        }

        public virtual void MoveTo(int index)
        {
            Volatile.Write(ref _value, index);
        }

        public void Unset()
        {
            Volatile.Write(ref _action, (int) PendingAction.None);
            MoveTo(UNSET_ID); // Ensure MoveTo overrides are called.
        }

        public enum PendingAction
        {
            None,
            Add,
            Schedule,
            Remove
        }
    }
}
