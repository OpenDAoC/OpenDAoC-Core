using System.Threading;

namespace DOL.GS
{
    public class MovementComponent : IServiceObject
    {
        private const int SUBZONE_RELOCATION_CHECK_INTERVAL = 500;

        private Point2D _lastPosition = new();
        private bool _relocationPending;
        private long _nextRelocationCheckTick;
        private int _turningDisabledCount;

        public GameLiving Owner { get; }
        public short CurrentSpeed { get; set; }
        public short MaxSpeedBase { get; set; } // Currently unused for players.
        public long LastMovementTick { get; private set; }
        public virtual short MaxSpeed => (short) Owner.GetModified(eProperty.MaxSpeed);
        public bool IsMoving => CurrentSpeed != 0;
        public bool IsTurningDisabled => Interlocked.CompareExchange(ref _turningDisabledCount, 0, 0) > 0 && !Owner.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound);
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.MovementComponent);

        protected MovementComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public static MovementComponent Create(GameLiving living)
        {
            if (living is GameNPC npc)
                return new NpcMovementComponent(npc);
            else if (living is GamePlayer player)
                return new PlayerMovementComponent(player);
            else
                return new MovementComponent(living);
        }

        public virtual void Tick()
        {
            if (Owner.ObjectState is not GameObject.eObjectState.Active)
            {
                RemoveFromServiceObjectStore();
                return;
            }

            TickInternal();
        }

        public virtual void DisableTurning(bool add)
        {
            if (add)
                Interlocked.Increment(ref _turningDisabledCount);
            else
                Interlocked.Decrement(ref _turningDisabledCount);
        }

        protected virtual void TickInternal()
        {
            if (!Owner.IsSamePosition(_lastPosition))
            {
                _lastPosition.X = Owner.X;
                _lastPosition.Y = Owner.Y;
                _relocationPending = true;
                LastMovementTick = GameLoop.GameLoopTime;
                OnOwnerMoved();
            }

            if (_relocationPending && GameServiceUtils.ShouldTick(_nextRelocationCheckTick))
            {
                Owner.SubZoneObject.CheckForRelocation();
                _nextRelocationCheckTick = GameLoop.GameLoopTime + SUBZONE_RELOCATION_CHECK_INTERVAL;
                _relocationPending = false;
            }
        }

        protected virtual void OnOwnerMoved() { }

        protected void AddToServiceObjectStore()
        {
            ServiceObjectStore.Add(this);
        }

        protected void RemoveFromServiceObjectStore()
        {
            ServiceObjectStore.Remove(this);
        }
    }
}
