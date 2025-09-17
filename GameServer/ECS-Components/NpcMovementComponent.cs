using System;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;
using DOL.GS.ServerProperties;
using DOL.Logging;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcMovementComponent : MovementComponent
    {
        public static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const short DEFAULT_WALK_SPEED = 70;

        private MovementState _movementState;
        private Vector3 _velocity;
        private Vector3 _destination;
        private long _nextFollowTick;
        private int _followTickInterval;
        private short _moveOnPathSpeed;
        private long _stopAtWaypointUntil;
        private long _walkingToEstimatedArrivalTime;
        private readonly MovementRequest _movementRequest = new();
        private readonly PathCalculator _pathCalculator;
        private ResetHeadingAction _resetHeadingAction;
        private Vector3 _destinationForClient;
        private long _positionForClientTick;
        private Vector3 _positionForClient;
        private bool _needsBroadcastUpdate = true;
        private short _currentMovementDesiredSpeed;
        private PathVisualization _pathVisualization;
        private long _lastPositionUpdateTick = -1;

        public new GameNPC Owner { get; }
        public ref Vector3 Velocity => ref _velocity;
        public ref Vector3 Destination => ref _destination;
        public GameLiving FollowTarget { get; private set; }
        public int MinFollowDistance { get; private set; } = 100;
        public int MaxFollowDistance { get; private set; } = 3000;
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
        public bool IsAtDestination => !IsDestinationValid || (_destination - _ownerPosition).LengthSquared() < 1.0f;
        public bool CanRoam => Properties.ALLOW_ROAM && RoamingRange > 0 && string.IsNullOrWhiteSpace(PathID);
        public double HorizontalVelocityForClient { get; private set; }
        public bool HasActiveResetHeadingAction => _resetHeadingAction != null && _resetHeadingAction.IsAlive;
        public ref Vector3 DestinationForClient => ref _destinationForClient;
        public ref Vector3 PositionForClient => ref _positionForClientTick == GameLoop.GameLoopTime ? ref _positionForClient : ref _ownerPosition;

        public int X
        {
            get
            {
                UpdatePosition();
                return (int) Math.Round(_ownerPosition.X);
            }
        }

        public int Y
        {
            get
            {
                UpdatePosition();
                return (int) Math.Round(_ownerPosition.Y);
            }
        }

        public int Z
        {
            get
            {
                UpdatePosition();
                return (int) Math.Round(_ownerPosition.Z);
            }
        }

        public NpcMovementComponent(GameNPC owner) : base(owner)
        {
            Owner = owner;
            _pathCalculator = new(owner);
        }

        protected override void TickInternal()
        {
            // Update the component's position first for correct calculations.
            UpdatePosition();

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
                ProcessMovementRequest();
            }

            if (IsFlagSet(MovementState.FOLLOW))
            {
                if (GameServiceUtils.ShouldTick(_nextFollowTick))
                {
                    _followTickInterval = FollowTick();

                    if (_followTickInterval != 0)
                        _nextFollowTick = GameLoop.GameLoopTime + _followTickInterval;
                    else
                        UnsetFlag(MovementState.WALK_TO);
                }
            }

            if (IsFlagSet(MovementState.WALK_TO))
            {
                if (GameServiceUtils.ShouldTick(_walkingToEstimatedArrivalTime))
                {
                    UnsetFlag(MovementState.WALK_TO);
                    OnArrival();
                }
            }

            if (IsFlagSet(MovementState.AT_WAYPOINT))
            {
                if (GameServiceUtils.ShouldTick(_stopAtWaypointUntil))
                {
                    UnsetFlag(MovementState.AT_WAYPOINT);
                    MoveToNextWaypoint();
                }
            }

            FinalizeTick();

            void FinalizeTick()
            {
                base.TickInternal();

                if (_needsBroadcastUpdate)
                {
                    _needsBroadcastUpdate = false;
                    OnPositionUpdate();
                    ClientService.UpdateNpcForPlayers(Owner);
                }

                if (_movementState is MovementState.NONE)
                    RemoveFromServiceObjectStore();
            }
        }

        public void WalkTo(Vector3 destination, short speed)
        {
            _movementRequest.Set(MovementRequestType.Walk, destination, speed);
            SetFlag(MovementState.REQUEST);
            AddToServiceObjectStore();
        }

        public void PathTo(Vector3 destination, short speed)
        {
            _movementRequest.Set(MovementRequestType.Path, destination, speed);
            SetFlag(MovementState.REQUEST);
            AddToServiceObjectStore();
        }

        public void StopMoving()
        {
            _movementState = MovementState.NONE;
            StopFollowing();
            StopMovingOnPath();
            CancelReturnToSpawnPoint();

            if (IsMoving)
                UpdateMovement(0);
        }

        public void Follow(GameLiving target, int minDistance, int maxDistance)
        {
            if (target == null || target.ObjectState is not eObjectState.Active || !Owner.castingComponent.IsAllowedToFollow(target))
                return;

            if (target != FollowTarget)
                _nextFollowTick = 0;

            FollowTarget = target;
            MinFollowDistance = minDistance;
            MaxFollowDistance = maxDistance;
            SetFlag(MovementState.FOLLOW);
            AddToServiceObjectStore();
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
                    if (log.IsErrorEnabled)
                        log.Error($"Called {nameof(MoveOnPath)} but PathID is null (NPC: {Owner})");

                    return;
                }

                CurrentWaypoint = MovementMgr.LoadPath(PathID);

                if (CurrentWaypoint == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Called {nameof(MoveOnPath)} but LoadPath returned null (PathID: {PathID}) (NPC: {Owner})");

                    return;
                }

                SetFlag(MovementState.ON_PATH);
                PathTo(new(CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
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

                PathTo(new(CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), Owner.MaxSpeed);
            }
            else if (log.IsErrorEnabled)
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
            PathTo(new(Owner.SpawnPoint.X, Owner.SpawnPoint.Y, Owner.SpawnPoint.Z), speed);
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
                Vector3? target = PathingMgr.Instance.GetRandomPoint(Owner.CurrentZone, new(Owner.SpawnPoint.X, Owner.SpawnPoint.Y, Owner.SpawnPoint.Z), maxRoamingRadius);

                if (target.HasValue)
                    PathTo(target.Value, speed);

                return;
            }

            maxRoamingRadius = Util.Random(maxRoamingRadius);
            double angle = Util.RandomDouble() * Math.PI * 2;
            double targetX = Owner.SpawnPoint.X + maxRoamingRadius * Math.Cos(angle);
            double targetY = Owner.SpawnPoint.Y + maxRoamingRadius * Math.Sin(angle);
            WalkTo(new Vector3((float) targetX, (float) targetY, Owner.SpawnPoint.Z), speed);
        }

        public void RestartCurrentMovement()
        {
            if (IsDestinationValid && !IsAtDestination)
                WalkToInternal(new(_destination.X, _destination.Y, _destination.Z), _currentMovementDesiredSpeed);
        }

        public void TurnTo(GameObject target, int duration = 0)
        {
            if (target == null || target.CurrentRegion != Owner.CurrentRegion)
                return;

            TurnTo(target.X, target.Y, duration);
        }

        public void TurnTo(int x, int y, int duration = 0)
        {
            TurnTo(Owner.GetHeading(x, y), duration);
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
            AddToServiceObjectStore();
        }

        public override void DisableTurning(bool add)
        {
            // Trigger an update to make sure the NPC properly starts or stops auto facing client side.
            // May technically be only necessary if the count is going from 0 to 1 or 1 to 0, but we're skipping that because it would need to be thread safe.

            _needsBroadcastUpdate = true;
            base.DisableTurning(add);
        }

        public void TogglePathVisualization()
        {
            // Toggle both visualization for `PathCalculator` (pathfinding) and `PathPoint` (patrols, horse routes).

            _pathCalculator.ToggleVisualization();

            if (_pathVisualization != null)
            {
                _pathVisualization.CleanUp();
                _pathVisualization = null;
                return;
            }

            _pathVisualization = new();

            if (CurrentWaypoint != null)
                _pathVisualization.Visualize(MovementMgr.FindFirstPathPoint(CurrentWaypoint), Owner.CurrentRegion);
        }

        public void ForceUpdatePosition()
        {
            _lastPositionUpdateTick = -1;
            UpdatePosition();
            _positionForClient = _ownerPosition;
        }

        protected override void UpdatePosition()
        {
            if (_lastPositionUpdateTick == GameLoop.GameLoopTime)
                return;

            // We still update `_ownerPosition` if the NPC isn't moving in case it's not moving by itself (teleports, GM tool...)
            // This ensures it always has a value.
            if (!IsMoving)
            {
                _ownerPosition = new(Owner.RealX, Owner.RealY, Owner.RealZ);
                _lastPositionUpdateTick = GameLoop.GameLoopTime;
                return;
            }

            long timeDelta = GameLoop.GameLoopTime - _lastPositionUpdateTick;
            Vector3 movementDelta = _velocity * (timeDelta * 0.001f);
            Vector3 potentialPosition = _ownerPosition + movementDelta;

            if (!IsDestinationValid)
            {
                _ownerPosition = potentialPosition;
                _lastPositionUpdateTick = GameLoop.GameLoopTime;
                return;
            }

            Vector3 absToDestination = Vector3.Abs(_destination - _ownerPosition);
            Vector3 absMovementDelta = Vector3.Abs(movementDelta);

            // Create a "mask" vector (1.0f or 0.0f) for each axis.
            // 1.0f means we use the potential position.
            // 0.0f means we have overshot and should clamp to destination.
            Vector3 usePotential = new(
                absToDestination.X >= absMovementDelta.X ? 1.0f : 0.0f,
                absToDestination.Y >= absMovementDelta.Y ? 1.0f : 0.0f,
                absToDestination.Z >= absMovementDelta.Z ? 1.0f : 0.0f
            );

            _ownerPosition = potentialPosition * usePotential + _destination * (Vector3.One - usePotential);
            _lastPositionUpdateTick = GameLoop.GameLoopTime;
        }

        private void ProcessMovementRequest()
        {
            if (_movementRequest.Type is MovementRequestType.Walk)
                WalkToInternal(_movementRequest.Destination, _movementRequest.Speed);
            else
                PathToInternal(_movementRequest.Destination, _movementRequest.Speed);
        }

        private void UpdateVelocity(float distanceToTarget)
        {
            MovementStartTick = GameLoop.GameLoopTime;

            if (!IsMoving || distanceToTarget < 1)
            {
                _velocity = Vector3.Zero;
                HorizontalVelocityForClient = 0.0;
                return;
            }

            if (!IsDestinationValid)
            {
                double heading = Owner.Heading * Point2D.HEADING_TO_RADIAN;
                _velocity = new((float) -Math.Sin(heading), (float) Math.Cos(heading), 0.0f);
            }
            else
            {
                Vector3 direction = _destination - _ownerPosition;
                float scale = CurrentSpeed / distanceToTarget;
                _velocity = direction * scale;
            }

            HorizontalVelocityForClient =  new Vector2(_velocity.X, _velocity.Y).Length();
            return;
        }

        private void WalkToInternal(Vector3 destination, short speed)
        {
            if (IsTurningDisabled)
                return;

            _currentMovementDesiredSpeed = speed;

            if (speed > MaxSpeed)
                speed = MaxSpeed;

            if (speed <= 0)
            {
                if (CurrentSpeed > 0)
                    UpdateMovement(0);

                return;
            }

            TurnTo(FollowTarget);
            float distanceToTarget = Owner.GetDistanceTo(destination);
            int ticksToArrive = (int) (distanceToTarget * 1000 / speed);

            if (ticksToArrive <= 0)
            {
                if (CurrentSpeed > 0)
                    UpdateMovement(0);

                return;
            }

            // Assume either the destination or speed has changed.
            UpdateMovement(destination, distanceToTarget, speed);
            SetFlag(MovementState.WALK_TO);
            _walkingToEstimatedArrivalTime = GameLoop.GameLoopTime + ticksToArrive;
        }

        private void PathToInternal(Vector3 destination, short speed)
        {
            if (_pathCalculator == null)
            {
                UnsetFlag(MovementState.PATHING);
                WalkToInternal(destination, speed);
                return;
            }

            if (!PathCalculator.ShouldPath(Owner, destination))
            {
                UnsetFlag(MovementState.PATHING);
                WalkToInternal(destination, speed);
                return;
            }

            Vector3? nextNode = _pathCalculator.CalculateNextTarget(destination, out ENoPathReason noPathReason);

            // Fall back to normal walking method if no path is found.
            if (noPathReason is ENoPathReason.NoPath or ENoPathReason.End)
            {
                UnsetFlag(MovementState.PATHING);
                WalkToInternal(destination, speed);
                return;
            }

            // Pause movement and turn toward the destination the path contains a closed door.
            if (noPathReason is ENoPathReason.ClosedDoor)
            {
                TurnTo((int) destination.X, (int) destination.Y);
                UnsetFlag(MovementState.PATHING);

                if (IsMoving)
                    UpdateMovement(0);

                return;
            }

            // Walk towards the next pathing node.
            _movementRequest.Set(MovementRequestType.Path, destination, speed);
            SetFlag(MovementState.PATHING);
            WalkToInternal(nextNode.Value, speed);
            return;
        }

        private int FollowTick()
        {
            // Stop moving if the NPC is casting or using ranged weapons.
            if (Owner.IsCasting || (Owner.IsAttacking && Owner.ActiveWeaponSlot is eActiveWeaponSlot.Distance))
            {
                StopMoving();
                return Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            if (!FollowTarget.IsAlive || FollowTarget.ObjectState is not eObjectState.Active || Owner.CurrentRegionID != FollowTarget.CurrentRegionID)
            {
                StopMoving();
                return 0;
            }

            Vector3 targetPos = new(FollowTarget.X, FollowTarget.Y, FollowTarget.Z);

            if (Owner.Brain is StandardMobBrain brain && FollowTarget.Realm == Owner.Realm)
            {
                int tx = (int) targetPos.X;
                int ty = (int) targetPos.Y;
                int tz = (int) targetPos.Z;

                // Update to formation-adjusted position.
                if (brain.CheckFormation(ref tx, ref ty, ref tz))
                {
                    targetPos = new(tx, ty, tz);
                    MinFollowDistance = 0;
                }
            }

            Vector3 relative = targetPos - _ownerPosition;
            float distanceSquared = relative.LengthSquared();
            long maxFollowDistanceSquared = MaxFollowDistance * MaxFollowDistance;

            if (distanceSquared > maxFollowDistanceSquared)
            {
                ReturnToSpawnPoint();
                return 0;
            }

            if (distanceSquared <= MinFollowDistance * MinFollowDistance)
            {
                TurnTo(FollowTarget);

                if (IsMoving)
                    UpdateMovement(0);

                return Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            float distance = MathF.Sqrt(distanceSquared);

            Vector3 direction = Vector3.Normalize(relative);
            Vector3 offset = direction * MinFollowDistance;
            Vector3 destination = targetPos - offset;

            short speed;

            // No smoothing if the NPC is attacking and is out of melee range.
            if (Owner.IsAttacking && distance > Owner.MeleeAttackRange)
                speed = MaxSpeed;
            else
                speed = (short) Math.Min(MaxSpeed, (distance - MinFollowDistance) * 2.5);

            PathToInternal(destination, Math.Max((short) 10, speed));
            return Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        private void OnArrival()
        {
            if (IsFlagSet(MovementState.PATHING))
            {
                ProcessMovementRequest();
                return;
            }

            if (IsFlagSet(MovementState.FOLLOW))
                return;

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
                _ownerPosition = _destination;
                UpdateMovement(0);
            }
        }

        private void MoveToNextWaypoint()
        {
            PathPoint oldPathPoint = CurrentWaypoint;
            PathPoint nextPathPoint = CurrentWaypoint.Next;

            if ((CurrentWaypoint.Type is EPathType.Path_Reverse) && CurrentWaypoint.FiredFlag)
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
                CurrentWaypoint = CurrentWaypoint.Type is EPathType.Path_Reverse && CurrentWaypoint.FiredFlag ? CurrentWaypoint.Prev : CurrentWaypoint.Next;

            oldPathPoint.FiredFlag = !oldPathPoint.FiredFlag;

            if (CurrentWaypoint != null)
                PathTo(new(CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
            else
                StopMovingOnPath();
        }

        private void PrepareValuesForClient(bool wasMoving, double distanceToTarget)
        {
            // Use slightly modified object position and target position to smooth movement out client-side.
            // The real target position makes NPCs stop before it. The real object position makes NPCs teleport a bit ahead when initiating movement.
            // The reasons why it happens and the expected values by the client are unknown.
            _positionForClientTick = GameLoop.GameLoopTime;

            if (!IsDestinationValid)
            {
                _positionForClient = _ownerPosition;
                _destinationForClient = Destination;
                return;
            }

            if (wasMoving)
                _positionForClient = _ownerPosition;
            else
            {
                float magic = (float) (CurrentSpeed * 0.15);
                float ratio = (float) ((distanceToTarget + magic) / distanceToTarget);
                _positionForClient = Vector3.Lerp(_destination, _ownerPosition, ratio);
            }

            if (distanceToTarget < 1)
                _destinationForClient = _ownerPosition;
            else
            {
                float magic = (float) Math.Max(15, CurrentSpeed * 0.1);
                float ratio = (float) ((distanceToTarget + magic) / distanceToTarget);
                _destinationForClient = Vector3.Lerp(_ownerPosition, _destination, ratio);
            }
        }

        private void UpdateMovement(short speed)
        {
            // Save current position.
            Owner.X = (int) Math.Round(_ownerPosition.X);
            Owner.Y = (int) Math.Round(_ownerPosition.Y);
            Owner.Z = (int) Math.Round(_ownerPosition.Z);

            _needsBroadcastUpdate = true;
            IsDestinationValid = false;
            bool wasMoving = IsMoving;
            CurrentSpeed = speed;
            UpdateVelocity(0);
            PrepareValuesForClient(wasMoving, 0);
        }

        private void UpdateMovement(Vector3 destination, float distanceToTarget, short speed)
        {
            // Save current position.
            Owner.X = (int) Math.Round(_ownerPosition.X);
            Owner.Y = (int) Math.Round(_ownerPosition.Y);
            Owner.Z = (int) Math.Round(_ownerPosition.Z);

            IsDestinationValid = distanceToTarget >= 1;
            _destination = destination;
            _needsBroadcastUpdate = true;

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

        private delegate void MovementRequestAction(Vector3 destination, short speed);

        private enum MovementRequestType
        {
            Walk,
            Path
        }

        private class MovementRequest
        {
            public MovementRequestType Type { get; private set; }
            public Vector3 Destination { get; private set; }
            public short Speed { get; private set; }

            public void Set(MovementRequestType type, Vector3 destination, short speed)
            {
                Type = type;
                Destination = destination;
                Speed = speed;
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
                    owner.ObjectState is eObjectState.Active &&
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
