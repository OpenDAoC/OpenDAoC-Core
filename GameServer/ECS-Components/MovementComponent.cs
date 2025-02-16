using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public class MovementComponent
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const int SUBZONE_RELOCATION_CHECK_INTERVAL = 500;

        private long _nextSubZoneRelocationCheckTick;
        private Point2D _positionDuringLastSubZoneRelocationCheck = new();
        private int _turningDisabledCount;

        public GameLiving Owner { get; }
        public short CurrentSpeed { get; set; }
        public short MaxSpeedBase { get; set; } // Currently unused for players.
        public virtual short MaxSpeed => (short) Owner.GetModified(eProperty.MaxSpeed);
        public bool IsMoving => CurrentSpeed != 0;
        public bool IsTurningDisabled => Interlocked.CompareExchange(ref _turningDisabledCount, 0, 0) > 0 && !Owner.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound);

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

        public void Tick()
        {
            long startTick = GameLoop.GetCurrentTime();
            TickInternal();
            long stopTick = GameLoop.GetCurrentTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {nameof(MovementComponent)}.{nameof(TickInternal)} for {Owner.Name}({Owner.ObjectID}) Time: {stopTick - startTick}ms");
        }

        protected virtual void TickInternal()
        {
            // Only check for subzone relocation if we moved.
            if (!Owner.IsSamePosition(_positionDuringLastSubZoneRelocationCheck) && ServiceUtils.ShouldTickAdjust(ref _nextSubZoneRelocationCheckTick))
            {
                _nextSubZoneRelocationCheckTick += SUBZONE_RELOCATION_CHECK_INTERVAL;
                _positionDuringLastSubZoneRelocationCheck = new Point2D(Owner.X, Owner.Y);
                Owner.SubZoneObject.CheckForRelocation();
            }
        }

        public virtual void DisableTurning(bool add)
        {
            if (add)
                Interlocked.Increment(ref _turningDisabledCount);
            else
                Interlocked.Decrement(ref _turningDisabledCount);
        }
    }
}
