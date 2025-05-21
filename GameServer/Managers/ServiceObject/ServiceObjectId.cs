namespace DOL.GS
{
    public class ServiceObjectId
    {
        public const int UNSET_ID = -1;
        private int _value = UNSET_ID;
        private PendingState _pendingState = PendingState.None;

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _pendingState = PendingState.None;
            }
        }

        public ServiceObjectType Type { get; }
        public bool IsSet => _value > UNSET_ID;
        public bool IsPendingAddition => _pendingState == PendingState.Adding;
        public bool IsPendingRemoval => _pendingState == PendingState.Removing;

        public ServiceObjectId(ServiceObjectType type)
        {
            Type = type;
        }

        public void OnPreAdd()
        {
            _pendingState = PendingState.Adding;
        }

        public void OnPreRemove()
        {
            _pendingState = PendingState.Removing;
        }

        public void Unset()
        {
            Value = UNSET_ID;
        }

        private enum PendingState
        {
            None,
            Adding,
            Removing
        }
    }
}
