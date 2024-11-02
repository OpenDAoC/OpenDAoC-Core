using System;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;
using DOL.GS.ServerProperties;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcMovementComponent : MovementComponent
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const short DEFAULT_WALK_SPEED = 70;
        public const int MIN_ALLOWED_FOLLOW_DISTANCE = 100;
        public const int MIN_ALLOWED_PET_FOLLOW_DISTANCE = 90;
        private const double FOLLOW_SPEED_SCALAR = 2.5;

        private MovementState _movementState;
        private long _nextFollowTick;
        private int _followTickInterval;
        private short _moveOnPathSpeed;
        private long _stopAtWaypointUntil;
        private long _walkingToEstimatedArrivalTime;
        private MovementRequest _movementRequest;
        private PathCalculator _pathCalculator;
        private ResetHeadingAction _resetHeadingAction;
        private Point3D _positionForUpdatePackets;
        private bool _needsBroadcastUpdate;
        private short _currentMovementDesiredSpeed;

        public new GameNPC Owner { get; }
        public Vector3 Velocity { get; private set; }
        public Point3D Destination { get; private set; }
        public GameLiving FollowTarget { get; private set; }
        public int FollowMinDistance { get; private set; } = 100;
        public int FollowMaxDistance { get; private set; } = 3000;
        public string PathID { get; set; }
        public PathPoint CurrentWaypoint { get; set; }
        public bool IsReturningToSpawnPoint { get; private set; }
        public int RoamingRange { get; set; }
        public long MovementStartTick { get; set; }
        public long MovementElapsedTicks => IsMoving ? GameLoop.GameLoopTime - MovementStartTick : 0;
        public bool FixedSpeed { get; set; }
        public override short MaxSpeed => FixedSpeed ? MaxSpeedBase : base.MaxSpeed;
        public bool IsMovingOnPath => IsFlagSet(MovementState.ON_PATH);
        public bool IsNearSpawn => Owner.IsWithinRadius(Owner.SpawnPoint, 25);
        public bool IsDestinationValid { get; private set; }
        public bool IsAtDestination => !IsDestinationValid || (Destination.X == Owner.X && Destination.Y == Owner.Y && Destination.Z == Owner.Z);
        public bool CanRoam => Properties.ALLOW_ROAM && RoamingRange > 0 && string.IsNullOrWhiteSpace(PathID);
        public double HorizontalVelocityForClient { get; private set; }
        public Point3D PositionForClient => _needsBroadcastUpdate ? _positionForUpdatePackets : Owner;
        public bool HasActiveResetHeadingAction => _resetHeadingAction != null && _resetHeadingAction.IsAlive;
        public Point3D DestinationForClient { get; private set; }

        public NpcMovementComponent(GameNPC owner) : base(owner)
        {
            Owner = owner;
            _pathCalculator = new(owner);
            _positionForUpdatePackets = Owner;
        }

        public override void Tick()
        {
            if (IsFlagSet(MovementState.TURN_TO))
            {
                if (!Owner.IsAttacking)
                {
                    FinalizeTick();
                    return;
                }

                UnsetFlag(MovementState.TURN_TO);
                _resetHeadingAction.Stop();
                _resetHeadingAction = null;
            }

            if (IsFlagSet(MovementState.REQUEST))
            {
                UnsetFlag(MovementState.REQUEST);
                _movementRequest.Execute();
            }

            if (IsFlagSet(MovementState.FOLLOW))
            {
                if (ServiceUtils.ShouldTickAdjust(ref _nextFollowTick))
                {
                    _followTickInterval = FollowTick();

                    if (_followTickInterval != 0)
                        _nextFollowTick += _followTickInterval;
                    else
                        UnsetFlag(MovementState.WALK_TO);
                }
            }

            if (IsFlagSet(MovementState.WALK_TO))
            {
                if (ServiceUtils.ShouldTick(_walkingToEstimatedArrivalTime))
                {
                    UnsetFlag(MovementState.WALK_TO);
                    OnArrival();
                }
            }

            if (IsFlagSet(MovementState.AT_WAYPOINT))
            {
                if (ServiceUtils.ShouldTick(_stopAtWaypointUntil))
                {
                    UnsetFlag(MovementState.AT_WAYPOINT);
                    MoveToNextWaypoint();
                }
            }

            FinalizeTick();

            void FinalizeTick()
            {
                base.Tick();

                if (_needsBroadcastUpdate)
                {
                    ClientService.UpdateNpcForPlayers(Owner);
                    _needsBroadcastUpdate = false;
                }
            }
        }

        public void WalkTo(Point3D destination, short speed)
        {
            // The copy is intentional. `Point3D` can be moving objects.
            destination = new Point3D(destination.X, destination.Y, destination.Z);
            _movementRequest = new(destination, speed, WalkToInternal);
            SetFlag(MovementState.REQUEST);
        }

        public void PathTo(Point3D destination, short speed)
        {
            // The copy is intentional. `Point3D` can be moving objects.
            destination = new Point3D(destination.X, destination.Y, destination.Z);
            _movementRequest = new(destination, speed, PathToInternal);
            SetFlag(MovementState.REQUEST);
        }

        public void StopMoving()
        {
            _movementState = MovementState.NONE;
            StopFollowing();
            StopMovingOnPath();
            CancelReturnToSpawnPoint();

            if (IsMoving)
                UpdateMovement(null, 0.0, 0);
        }

        public void Follow(GameLiving target, int minDistance, int maxDistance)
        {
            if (target == null || target.ObjectState != eObjectState.Active)
                return;

            if (target != FollowTarget)
                _nextFollowTick = 0;

            FollowTarget = target;
            FollowMinDistance = minDistance;
            FollowMaxDistance = maxDistance;
            SetFlag(MovementState.FOLLOW);
        }

        public void StopFollowing()
        {
            UnsetFlag(MovementState.FOLLOW);
            FollowTarget = null;
        }

        public void MoveOnPath(short speed)
        {
            StopMoving();
            _moveOnPathSpeed = speed;

            // Move to the first waypoint if we don't have any.
            // Otherwise and if we're not currently moving on path, move to the previous one (current waypoint if none).
            if (CurrentWaypoint == null)
            {
                if (PathID == null)
                {
                    log.Error($"Called {nameof(MoveOnPath)} but PathID is null (NPC: {Owner})");
                    return;
                }

                CurrentWaypoint = MovementMgr.LoadPath(PathID);

                if (CurrentWaypoint == null)
                {
                    log.Error($"Called {nameof(MoveOnPath)} but LoadPath returned null (PathID: {PathID}) (NPC: {Owner})");
                    return;
                }

                SetFlag(MovementState.ON_PATH);
                PathTo(CurrentWaypoint, Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
                return;
            }
            else if (!IsFlagSet(MovementState.ON_PATH))
            {
                SetFlag(MovementState.ON_PATH);

                if (Owner.IsWithinRadius(CurrentWaypoint, 25))
                {
                    MoveToNextWaypoint();
                    return;
                }

                if (CurrentWaypoint.Type == EPathType.Path_Reverse && CurrentWaypoint.FiredFlag)
                {
                    if (CurrentWaypoint.Next != null)
                        CurrentWaypoint = CurrentWaypoint.Next;
                }
                else if (CurrentWaypoint.Prev != null)
                    CurrentWaypoint = CurrentWaypoint.Prev;

                PathTo(CurrentWaypoint, Owner.MaxSpeed);
            }
            else
                log.Error($"Called {nameof(MoveOnPath)} but both CurrentWaypoint and ON_PATH are already set. (NPC: {Owner})");
        }

        public void StopMovingOnPath()
        {
            // Without this, horses would be immediately removed since 'MoveOnPath' immediately calls 'StopMoving', which calls 'StopMovingOnPath'.
            if (IsFlagSet(MovementState.ON_PATH))
            {
                if (Owner is GameTaxi or GameTaxiBoat)
                    Owner.RemoveFromWorld();

                UnsetFlag(MovementState.ON_PATH);
            }
        }

        public void ReturnToSpawnPoint()
        {
            ReturnToSpawnPoint(DEFAULT_WALK_SPEED);
        }

        public void ReturnToSpawnPoint(short speed)
        {
            StopMoving();
            Owner.TargetObject = null;
            Owner.attackComponent.StopAttack();
            (Owner.Brain as StandardMobBrain)?.ClearAggroList();
            IsReturningToSpawnPoint = true;
            PathTo(Owner.SpawnPoint, speed);
        }

        public void CancelReturnToSpawnPoint()
        {
            IsReturningToSpawnPoint = false;
        }

        public void Roam(short speed)
        {
            // Note that `CanRoam` returns false if `RoamingRange` is <= 0.
            int maxRoamingRadius = Owner.RoamingRange > 0 ? Owner.RoamingRange : Owner.CurrentRegion.IsDungeon ? 5 : 500;

            if (Owner.CurrentZone.IsPathingEnabled)
            {
                Vector3? target = PathingMgr.Instance.GetRandomPointAsync(Owner.CurrentZone, new Vector3(Owner.SpawnPoint.X, Owner.SpawnPoint.Y, Owner.SpawnPoint.Z), maxRoamingRadius);

                if (target.HasValue)
                    PathTo(new Point3D(target.Value.X, target.Value.Y, target.Value.Z), speed);

                return;
            }

            maxRoamingRadius = Util.Random(maxRoamingRadius);
            double angle = Util.RandomDouble() * Math.PI * 2;
            double targetX = Owner.SpawnPoint.X + maxRoamingRadius * Math.Cos(angle);
            double targetY = Owner.SpawnPoint.Y + maxRoamingRadius * Math.Sin(angle);
            WalkTo(new Point3D((int) targetX, (int) targetY, Owner.SpawnPoint.Z), speed);
        }

        public void RestartCurrentMovement()
        {
            if (IsDestinationValid && !IsAtDestination)
                WalkToInternal(Destination, _currentMovementDesiredSpeed);
        }

        public void TurnTo(GameObject target, int duration = 0)
        {
            if (target == null || target.CurrentRegion != Owner.CurrentRegion)
                return;

            TurnTo(target.X, target.Y, duration);
        }

        public void TurnTo(int x, int y, int duration = 0)
        {
            TurnTo(Owner.GetHeading(new Point2D(x, y)), duration);
        }

        public void TurnTo(ushort heading, int duration = 0)
        {
            if (Owner.Heading == heading || Owner.IsStunned || Owner.IsMezzed || IsTurningDisabled)
                return;

            if (duration > 0)
            {
                SetFlag(MovementState.TURN_TO);

                if (_resetHeadingAction == null)
                {
                    _resetHeadingAction = CreateResetHeadingAction();
                    _resetHeadingAction.Start(duration);
                }
                else
                {
                    // Attempt to extend the duration of our existing `ResetHeadingAction`.
                    _resetHeadingAction.Start(duration);

                    if (!_resetHeadingAction.IsAlive)
                    {
                        _resetHeadingAction = CreateResetHeadingAction();
                        _resetHeadingAction.Start(duration);
                    }
                }
            }

            _needsBroadcastUpdate = true;
            Owner.Heading = heading;
        }

        public override void DisableTurning(bool add)
        {
            // Trigger an update to make sure the NPC properly starts or stops auto facing client side.
            // May technically be only necessary if the count is going from 0 to 1 or 1 to 0, but we're skipping that because it would needs to be thread safe.
            _needsBroadcastUpdate = true;
            base.DisableTurning(add);
        }

        private void UpdateVelocity(double distanceToTarget)
        {
            MovementStartTick = GameLoop.GameLoopTime;

            if (!IsMoving || distanceToTarget < 1)
            {
                Velocity = Vector3.Zero;
                HorizontalVelocityForClient = 0.0;
                return;
            }

            float velocityX;
            float velocityY;
            float velocityZ;

            if (!IsDestinationValid)
            {
                double heading = Owner.Heading * Point2D.HEADING_TO_RADIAN;
                velocityX = (float) -Math.Sin(heading);
                velocityY = (float) Math.Cos(heading);
                velocityZ = 0.0f;
            }
            else
            {
                velocityX = (float) ((Destination.X - Owner.RealX) / distanceToTarget * CurrentSpeed);
                velocityY = (float) ((Destination.Y - Owner.RealY) / distanceToTarget * CurrentSpeed);
                velocityZ = (float) ((Destination.Z - Owner.RealZ) / distanceToTarget * CurrentSpeed);
            }

            Velocity = new(velocityX, velocityY, velocityZ);
            HorizontalVelocityForClient = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);
            return;
        }

        private void WalkToInternal(Point3D destination, short speed)
        {
            if (IsTurningDisabled)
                return;

            _currentMovementDesiredSpeed = speed;

            if (speed > MaxSpeed)
                speed = MaxSpeed;

            if (destination == null || speed <= 0)
            {
                UpdateMovement(null, 0.0, speed);
                return;
            }

            int distanceToTarget = Owner.GetDistanceTo(destination);
            int ticksToArrive = distanceToTarget * 1000 / speed;

            if (ticksToArrive > 0)
            {
                if (distanceToTarget > 25)
                    TurnTo(destination.X, destination.Y);

                UpdateMovement(destination, distanceToTarget, speed);
                SetFlag(MovementState.WALK_TO);
                _walkingToEstimatedArrivalTime = GameLoop.GameLoopTime + ticksToArrive;
            }
            else
                UpdateMovement(null, 0.0, 0);
        }

        private void PathToInternal(Point3D destination, short speed)
        {
            // Pathing with no target position isn't currently supported.
            if (_pathCalculator == null || destination == null)
            {
                UnsetFlag(MovementState.PATHING);
                WalkToInternal(destination, speed);
                return;
            }

            Vector3 destinationForPathCalculator = new(destination.X, destination.Y, destination.Z);

            if (!PathCalculator.ShouldPath(Owner, destinationForPathCalculator))
            {
                UnsetFlag(MovementState.PATHING);
                WalkToInternal(destination, speed);
                return;
            }

            Tuple<Vector3?, NoPathReason> res = _pathCalculator.CalculateNextTarget(destinationForPathCalculator);
            Vector3? nextNode = res.Item1;
            //NoPathReason noPathReason = res.Item2;
            //bool shouldUseAirPath = noPathReason == NoPathReason.RECAST_FOUND_NO_PATH;
            //bool didFindPath = PathCalculator.DidFindPath;

            if (!nextNode.HasValue)
            {
                UnsetFlag(MovementState.PATHING);
                WalkToInternal(destination, speed);
                return;
            }

            // Do the actual pathing bit: Walk towards the next pathing node
            _movementRequest = new(destination, speed, PathToInternal);
            SetFlag(MovementState.PATHING);
            WalkToInternal(new Point3D(nextNode.Value.X, nextNode.Value.Y, nextNode.Value.Z), speed);
            return;
        }

        private int FollowTick()
        {
            // Stop moving if the NPC is casting or attacking with a ranged weapon.
            if (Owner.IsCasting || (Owner.IsAttacking && Owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance))
            {
                if (IsMoving)
                    StopMoving();

                return Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            if (!FollowTarget.IsAlive || FollowTarget.ObjectState != eObjectState.Active || Owner.CurrentRegionID != FollowTarget.CurrentRegionID)
            {
                StopFollowing();
                return 0;
            }

            int targetX = FollowTarget.X;
            int targetY = FollowTarget.Y;
            int targetZ = FollowTarget.Z;
            float relativeX;
            float relativeY;
            float relativeZ;
            double distance;
            bool isInFormation;

            if (Owner.Brain is StandardMobBrain brain && Owner.FollowTarget.Realm == Owner.Realm)
            {
                if (brain.CheckFormation(ref targetX, ref targetY, ref targetZ))
                    isInFormation = true;
                else
                    isInFormation = false;

                relativeX = targetX - Owner.X;
                relativeY = targetY - Owner.Y;
                relativeZ = targetZ - Owner.Z;
            }
            else
            {
                relativeX = FollowTarget.X - Owner.X;
                relativeY = FollowTarget.Y - Owner.Y;
                relativeZ = FollowTarget.Z - Owner.Z;
                isInFormation = false;
            }

            distance = Math.Sqrt(relativeX * relativeX + relativeY * relativeY + relativeZ * relativeZ);

            // If distance is greater then the max follow distance, stop following and return home.
            if (distance > FollowMaxDistance)
            {
                ReturnToSpawnPoint();
                return 0;
            }

            int minAllowedFollowDistance;

            if (isInFormation)
                minAllowedFollowDistance = 0;
            else
            {
                minAllowedFollowDistance = Math.Max(FollowMinDistance, MIN_ALLOWED_FOLLOW_DISTANCE);

                if (distance <= minAllowedFollowDistance)
                {
                    TurnTo(FollowTarget);

                    if (IsMoving)
                        UpdateMovement(null, 0.0, 0);

                    return Properties.GAMENPC_FOLLOWCHECK_TIME;
                }
            }

            // Use a slightly lower follow distance for destination calculation. This helps with heading at low speed.
            minAllowedFollowDistance = Math.Max(0, minAllowedFollowDistance - 10);
            relativeX = (float) (relativeX / distance * minAllowedFollowDistance);
            relativeY = (float) (relativeY / distance * minAllowedFollowDistance);
            relativeZ = (float) (relativeZ / distance * minAllowedFollowDistance);
            Point3D destination = new((int) (targetX - relativeX), (int) (targetY - relativeY), (int) (targetZ - relativeZ));
            short speed = (short) ((distance - minAllowedFollowDistance) * (1000.0 / Properties.GAMENPC_FOLLOWCHECK_TIME));
            PathToInternal(destination, Math.Min(MaxSpeed, speed));
            return Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        private void OnArrival()
        {
            if (IsFlagSet(MovementState.PATHING))
            {
                _movementRequest.Execute();
                return;
            }

            if (IsFlagSet(MovementState.FOLLOW))
            {
                FollowTick();
                return;
            }

            if (IsReturningToSpawnPoint)
            {
                SetPositionToDestination();
                CancelReturnToSpawnPoint();
                TurnTo(Owner.SpawnHeading);
                return;
            }

            if (IsFlagSet(MovementState.ON_PATH))
            {
                if (CurrentWaypoint != null)
                {
                    if (CurrentWaypoint.WaitTime == 0)
                    {
                        MoveToNextWaypoint();
                        return;
                    }

                    SetFlag(MovementState.AT_WAYPOINT);
                    _stopAtWaypointUntil = GameLoop.GameLoopTime + CurrentWaypoint.WaitTime * 100;
                }
                else
                    StopMovingOnPath();
            }

            if (IsMoving)
                SetPositionToDestination();

            void SetPositionToDestination()
            {
                Owner.X = Destination.X;
                Owner.Y = Destination.Y;
                Owner.Z = Destination.Z;
                UpdateMovement(null, 0.0, 0);
            }
        }

        private void MoveToNextWaypoint()
        {
            PathPoint oldPathPoint = CurrentWaypoint;
            PathPoint nextPathPoint = CurrentWaypoint.Next;

            if ((CurrentWaypoint.Type == EPathType.Path_Reverse) && CurrentWaypoint.FiredFlag)
                nextPathPoint = CurrentWaypoint.Prev;

            if (nextPathPoint == null)
            {
                switch (CurrentWaypoint.Type)
                {
                    case EPathType.Loop:
                    {
                        CurrentWaypoint = MovementMgr.FindFirstPathPoint(CurrentWaypoint);
                        break;
                    }
                    case EPathType.Once:
                    {
                        CurrentWaypoint = null;
                        PathID = null; // Unset the path ID, otherwise the brain will re-enter patrolling state and restart it.
                        break;
                    }
                    case EPathType.Path_Reverse:
                    {
                        CurrentWaypoint = oldPathPoint.FiredFlag ? CurrentWaypoint.Next : CurrentWaypoint.Prev;
                        break;
                    }
                }
            }
            else
                CurrentWaypoint = CurrentWaypoint.Type == EPathType.Path_Reverse && CurrentWaypoint.FiredFlag ? CurrentWaypoint.Prev : CurrentWaypoint.Next;

            oldPathPoint.FiredFlag = !oldPathPoint.FiredFlag;

            if (CurrentWaypoint != null)
                WalkToInternal(CurrentWaypoint, Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
            else
                StopMovingOnPath();
        }

        private void PrepareValuesForClient(bool wasMoving, double distanceToTarget)
        {
            // Use slightly modified object position and target position to smooth movement out client-side.
            // The real target position makes NPCs stop before it. The real object position makes NPCs teleport a bit ahead when initiating movement.
            // The reasons why it happens and the expected values by the client are unknown.

            if (!IsDestinationValid)
            {
                _positionForUpdatePackets = Owner;
                DestinationForClient = Destination;
                return;
            }

            double magic;
            double ratio;
            double complementRatio;

            if (wasMoving)
                _positionForUpdatePackets = Owner;
            else
            {
                magic = CurrentSpeed * 0.15;
                ratio = (distanceToTarget + magic) / distanceToTarget;
                complementRatio = 1 - ratio;

                _positionForUpdatePackets = new()
                {
                    X = (int) (complementRatio * Destination.X + ratio * Owner.RealX),
                    Y = (int) (complementRatio * Destination.Y + ratio * Owner.RealY),
                    Z = (int) (complementRatio * Destination.Z + ratio * Owner.RealZ)
                };
            }

            if (distanceToTarget < 1)
                DestinationForClient = Owner;
            else
            {
                magic = Math.Max(20, CurrentSpeed * 0.1);
                ratio = (distanceToTarget + magic) / distanceToTarget;
                complementRatio = 1 - ratio;

                DestinationForClient = new()
                {
                    X = (int) (complementRatio * Owner.RealX + ratio * Destination.X),
                    Y = (int) (complementRatio * Owner.RealY + ratio * Destination.Y),
                    Z = (int) (complementRatio * Owner.RealZ + ratio * Destination.Z)
                };
            }
        }

        private void UpdateMovement(Point3D destination, double distanceToTarget, short speed)
        {
            // Save current position.
            Owner.X = Owner.X;
            Owner.Y = Owner.Y;
            Owner.Z = Owner.Z;

            if (destination == null || distanceToTarget < 1)
            {
                _needsBroadcastUpdate = true;
                IsDestinationValid = false;
            }
            else
            {
                if (CurrentSpeed != speed || !Destination.IsSamePosition(destination))
                    _needsBroadcastUpdate = true;

                Destination = destination;
                IsDestinationValid = true;
            }

            bool wasMoving = IsMoving;
            CurrentSpeed = speed;
            UpdateVelocity(distanceToTarget);
            PrepareValuesForClient(wasMoving, distanceToTarget);
        }

        private ResetHeadingAction CreateResetHeadingAction()
        {
            return new(Owner, this, () =>
            {
                UnsetFlag(MovementState.TURN_TO);
                _resetHeadingAction = null;
            });
        }

        private bool IsFlagSet(MovementState flag)
        {
            return (_movementState & flag) == flag;
        }

        private void SetFlag(MovementState flag)
        {
            _movementState |= flag;
        }

        private void UnsetFlag(MovementState flag)
        {
            _movementState &= ~flag;
        }

        private delegate void MovementRequestAction(Point3D destination, short speed);

        private class MovementRequest
        {
            public Point3D Destination { get; }
            public short Speed { get; }
            public MovementRequestAction Action { get; }

            public MovementRequest(Point3D destination, short speed, MovementRequestAction action)
            {
                Destination = destination;
                Speed = speed;
                Action = action;
            }

            public void Execute()
            {
                Action(Destination, Speed);
            }
        }

        private class ResetHeadingAction : ECSGameTimerWrapperBase
        {
            private NpcMovementComponent _movementComponent;
            private ushort _oldHeading;
            private long _oldMovementStartTick;
            private Action _onCompletion;

            public ResetHeadingAction(GameObject actionSource, NpcMovementComponent movementComponent, Action onCompletion) : base(actionSource)
            {
                _movementComponent = movementComponent;
                _oldHeading = actionSource.Heading;
                _oldMovementStartTick = movementComponent.MovementStartTick;
                _onCompletion = onCompletion;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                GameNPC owner = _movementComponent.Owner;

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

        [Flags]
        private enum MovementState
        {
            NONE = 0,
            REQUEST = 1 << 1,      // Was requested to move.
            WALK_TO = 1 << 2,      // Is moving and has a destination.
            FOLLOW = 1 << 3,       // Is following an object.
            ON_PATH = 1 << 4,      // Is following a path / is patrolling.
            AT_WAYPOINT = 1 << 5,  // Is waiting at a waypoint.
            PATHING = 1 << 6,      // Is moving using PathCalculator.
            TURN_TO = 1 << 7       // Is facing a direction for a certain duration.
        }
    }
}
