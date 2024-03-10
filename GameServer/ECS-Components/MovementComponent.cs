namespace DOL.GS
{
    public class MovementComponent
    {
        private const int SUBZONE_RELOCATION_CHECK_INTERVAL = 500;

        private long _nextSubZoneRelocationCheckTick;
        private Point2D _positionDuringLastSubZoneRelocationCheck = new();
        private int _turningDisabledCount;

        public GameLiving Owner { get; private set; }
        public short CurrentSpeed { get; set; }
        public short MaxSpeedBase { get; set; } // Currently unused for players.
        public virtual short MaxSpeed => (short) Owner.GetModified(eProperty.MaxSpeed);
        public bool IsMoving => CurrentSpeed > 0;
        public bool IsTurningDisabled => _turningDisabledCount > 0 && !Owner.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound);

        protected MovementComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public static MovementComponent Create(GameLiving gameLiving)
        {
            if (gameLiving is GameNPC gameNpc)
                return new NpcMovementComponent(gameNpc);
            else
                return new MovementComponent(gameLiving);
        }

        public virtual void Tick()
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
                _turningDisabledCount++;
            else
                _turningDisabledCount--;

            if (_turningDisabledCount < 0)
                _turningDisabledCount = 0;
        }
    }
}
