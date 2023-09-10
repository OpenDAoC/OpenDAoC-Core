using System;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class MovementComponent
    {
        private const int SUBZONE_RELOCATION_CHECK_INTERVAL = 500;

        private long _lastSubZoneRelocationCheckTick;
        private Point3D _positionDuringLastSubZoneRelocationCheck = new();
        private int _turningDisabledCount;
        private AuxECSGameTimer _resetHeadingAction;

        public GameLiving Owner { get; private set; }
        public double TickSpeedX { get; private set; }
        public double TickSpeedY { get; private set; }
        public double TickSpeedZ { get; private set; }
        public bool FixedSpeed { get; set; }
        public short CurrentSpeed { get; set; }
        public short MaxSpeedBase { get; set; } // Currently unused for players.
        public long MovementStartTick { get; set; }
        public bool IsTurningDisabled => !Owner.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound) && _turningDisabledCount > 0;
        public short MaxSpeed => FixedSpeed ? MaxSpeedBase : (short) Owner.GetModified(eProperty.MaxSpeed);
        public bool IsMoving => CurrentSpeed > 0;
        public long MovementElapsedTicks => IsMoving ? GameLoop.GameLoopTime - MovementStartTick : 0;

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

        public virtual void Tick(long tick)
        {
            // Check for subzone relocation only if we're moving.
            if (!Owner.IsWithinRadius(_positionDuringLastSubZoneRelocationCheck, 0) && _lastSubZoneRelocationCheckTick + SUBZONE_RELOCATION_CHECK_INTERVAL < tick)
            {
                _lastSubZoneRelocationCheckTick = tick;
                _positionDuringLastSubZoneRelocationCheck = new Point3D(Owner.X, Owner.Y, Owner.Z);
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

        public virtual void TurnTo(GameObject target, int duration = 0)
        {
            if (target == null || target.CurrentRegion != Owner.CurrentRegion)
                return;

            TurnTo(target.X, target.Y, duration);
        }

        public virtual void TurnTo(int x, int y, int duration = 0)
        {
            TurnTo(Owner.GetHeading(new Point2D(x, y)), duration);
        }

        public virtual void TurnTo(ushort heading, int duration = 0)
        {
            if (Owner.IsStunned || Owner.IsMezzed)
                return;

            if (Owner.Heading != heading)
            {
                if (duration > 0 && _resetHeadingAction == null)
                {
                    _resetHeadingAction = new ResetHeadingAction(Owner, this, () => _resetHeadingAction = null);
                    _resetHeadingAction.Start(duration);
                }

                Owner.Heading = heading;
            }
        }

        protected virtual void UpdateTickSpeed()
        {
            if (!IsMoving)
            {
                SetTickSpeed(0.0, 0.0, 0.0);
                return;
            }

            double heading = Owner.Heading * Point2D.HEADING_TO_RADIAN;
            double tickSpeed = CurrentSpeed * 0.001;
            SetTickSpeed(-Math.Sin(heading) * tickSpeed, Math.Cos(heading) * tickSpeed, 0);
        }

        protected void SetTickSpeed(double x, double y, double z)
        {
            TickSpeedX = x;
            TickSpeedY = y;
            TickSpeedZ = z;
        }

        private class ResetHeadingAction : AuxECSGameTimerWrapperBase
        {
            private MovementComponent _movementComponent;
            private ushort _oldHeading;
            private long _oldMovementStartTick;
            private Action _onCompletion;

            public ResetHeadingAction(GameObject actionSource, MovementComponent movementComponent, Action onCompletion) : base(actionSource)
            {
                _movementComponent = movementComponent;
                _oldHeading = actionSource.Heading;
                _oldMovementStartTick = movementComponent.MovementStartTick;
                _onCompletion = onCompletion;
            }

            protected override int OnTick(AuxECSGameTimer timer)
            {
                GameLiving owner = _movementComponent.Owner;

                if (_oldMovementStartTick == _movementComponent.MovementStartTick &&
                    !_movementComponent.IsMoving &&
                    owner.IsAlive &&
                    owner.ObjectState == eObjectState.Active &&
                    !owner.attackComponent.AttackState)
                {
                    _movementComponent.TurnTo(_oldHeading);
                }

                _onCompletion();
                return 0;
            }
        }
    }
}
