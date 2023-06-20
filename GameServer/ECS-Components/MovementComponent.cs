using System;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class MovementComponent
    {
        private int _turningDisabledCount;
        private ushort _oldHeading;
        private long _oldMovementStartTick;
        private long _restoreOldHeadingAtTick;

        public GameLiving Owner { get; private set; }
        public double TickSpeedX { get; private set; }
        public double TickSpeedY { get; private set; }
        public double TickSpeedZ { get; private set; }
        public bool FixedSpeed { get; set; }
        public short CurrentSpeed { get; set; }
        public short MaxSpeedBase { get; set; }
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
            if (_restoreOldHeadingAtTick <= tick)
                RestoreOldHeading();
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
                Owner.Heading = heading;

                if (duration > 0)
                {
                    _oldHeading = heading;
                    _oldMovementStartTick = MovementStartTick;
                    _restoreOldHeadingAtTick = GameLoop.GameLoopTime + duration;
                }
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

        private void RestoreOldHeading()
        {
            if (_oldMovementStartTick == MovementStartTick &&
                !IsMoving &&
                Owner.IsAlive &&
                Owner.ObjectState == eObjectState.Active &&
                !Owner.attackComponent.AttackState)
            {
                TurnTo(_oldHeading);
            }
        }
    }
}
