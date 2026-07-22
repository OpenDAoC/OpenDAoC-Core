using System;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;
using DOL.GS.ServerProperties;
using DOL.Logging;
using OpenDAoC.Pathing;
using static DOL.GS.GameObject;
using static DOL.GS.Pathfinder;

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
        private long _stopAtPathPointUntil;
        private long _walkingToEstimatedArrivalTime;
        private readonly MovementRequest _movementRequest = new();
        private readonly Pathfinder _pathfinder;
        private ResetHeadingTimer _resetHeadingTimer;
        private Vector3 _destinationForClient;
        private long _positionForClientTick;
        private Vector3 _positionForClient;
        private bool _needsBroadcastUpdate = true;
        private short _currentMovementDesiredSpeed;
        private PathVisualization _pathVisualization;
        private long _lastPositionUpdateTick = -1;
        private Path _path = Path.None;
        private AntiExploitImmunityTimer _antiExploitImmunity;

        public new GameNPC Owner { get; }
        public ref Vector3 Velocity => ref _velocity;
        public ref Vector3 Destination => ref _destination;
        public GameLiving FollowTarget { get; private set; }
        public long MinFollowDistance { get; private set; }
        public long MaxFollowDistance { get; private set; }
        public int RoamingRange { get; set; }
        public long MovementStartTick { get; set; }
        public long MovementElapsedTicks => IsMoving ? GameLoop.GameLoopTime - MovementStartTick : 0;
        public bool FixedSpeed { get; set; }
        public override short MaxSpeed => FixedSpeed ? MaxSpeedBase : base.MaxSpeed;
        public bool IsMovingOnPath => IsFlagSet(MovementState.OnPath);
        public bool IsDestinationValid { get; private set; }
        public bool IsAtDestination => !IsDestinationValid || (_destination - _ownerPosition).LengthSquared() < 1.0f;
        public bool CanRoam => Properties.ALLOW_ROAM && RoamingRange > 0 && !CanMoveOnPath;
        public bool CanMoveOnPath => !_path.IsNone;
        public double HorizontalVelocityForClient { get; private set; }
        public bool HasActiveResetHeadingTimer => _resetHeadingTimer?.IsAlive == true;
        public bool IsPathVisualizationActive => _pathVisualization != null;
        public bool ShouldForceFlyingFlag => _pathfinder.IsJumping || IsPathVisualizationActive;
        public ref Vector3 DestinationForClient => ref _destinationForClient;
        public ref Vector3 PositionForClient => ref _positionForClientTick == GameLoop.GameLoopTime ? ref _positionForClient : ref _ownerPosition;

        public string PathId
        {
            get => _path.Id;
            set => _path = Path.WithPathId(_path, value);
        }

        public PathPoint CurrentPathPoint
        {
            get => _path.Point;
            set => _path = Path.WithPathPoint(_path, value);
        }

        public int X => (int) Math.Round(_ownerPosition.X);
        public int Y => (int) Math.Round(_ownerPosition.Y);
        public int Z => (int) Math.Round(_ownerPosition.Z);

        public NpcMovementComponent(GameNPC owner) : base(owner)
        {
            Owner = owner;
            _pathfinder = new(owner);
        }

        protected override void TickInternal()
        {
            // Update the component's position first for correct calculations.
            UpdatePosition();

            if (IsFlagSet(MovementState.TurnTo))
            {
                if (!Owner.IsAttacking)
                {
                    FinalizeTick();
                    return;
                }

                UnsetFlag(MovementState.TurnTo);
                _resetHeadingTimer?.Stop();
                _resetHeadingTimer = null;
            }

            if (IsFlagSet(MovementState.Request))
            {
                UnsetFlag(MovementState.Request);
                ProcessMovementRequest();
            }

            if (IsFlagSet(MovementState.Follow))
            {
                if (GameServiceUtils.ShouldTick(_nextFollowTick))
                {
                    _followTickInterval = FollowTick();

                    if (_followTickInterval != 0)
                        _nextFollowTick = GameLoop.GameLoopTime + _followTickInterval;
                    else
                        UnsetFlag(MovementState.WalkTo);
                }
            }

            if (IsFlagSet(MovementState.WalkTo))
            {
                if (GameServiceUtils.ShouldTick(_walkingToEstimatedArrivalTime))
                {
                    UnsetFlag(MovementState.WalkTo);
                    OnArrival();
                }
            }

            if (IsFlagSet(MovementState.AtPathPoint))
            {
                if (GameServiceUtils.ShouldTick(_stopAtPathPointUntil))
                {
                    UnsetFlag(MovementState.AtPathPoint);
                    MoveToNextPathPoint();
                }
            }

            FinalizeTick();
        }

        private void FinalizeTick()
        {
            base.TickInternal();

            Owner.X = (int) _ownerPosition.X;
            Owner.Y = (int) _ownerPosition.Y;
            Owner.Z = (int) _ownerPosition.Z;

            if (_needsBroadcastUpdate)
            {
                _needsBroadcastUpdate = false;
                UpdateLastMovementTick();
                ClientService.UpdateNpcForPlayers(Owner);
            }

            if (_movementState is MovementState.None)
                RemoveFromServiceObjectStore();
        }

        public void WalkTo(Vector3 destination, short speed)
        {
            _movementRequest.Set(MovementRequestType.Walk, destination, speed);
            SetFlag(MovementState.Request);
            AddToServiceObjectStore();
        }

        public void PathTo(Vector3 destination, short speed)
        {
            _movementRequest.Set(MovementRequestType.Path, destination, speed);
            SetFlag(MovementState.Request);
            AddToServiceObjectStore();
        }

        public void StopMoving()
        {
            _movementState = MovementState.None;
            StopFollowing();
            StopMovingOnPath();

            if (IsMoving)
                UpdateMovement(0);
        }

        public void Follow(GameLiving target, long minDistance, long maxDistance)
        {
            if (target == null || target.ObjectState is not eObjectState.Active || !Owner.IsAllowedToFollow(target))
                return;

            if (target != FollowTarget)
                _nextFollowTick = 0;

            FollowTarget = target;
            MinFollowDistance = minDistance;
            MaxFollowDistance = maxDistance;
            SetFlag(MovementState.Follow);
            AddToServiceObjectStore();
        }

        public void StopFollowing()
        {
            UnsetFlag(MovementState.Follow);
            FollowTarget = null;
        }

        public void MoveOnPath(short speed)
        {
            StopMoving();
            _moveOnPathSpeed = speed;

            // Move to the first path point if we don't have any.
            // Otherwise and if we're not currently moving on path, move to the previous one (current path point if none).
            if (CurrentPathPoint == null)
            {
                if (string.IsNullOrEmpty(PathId))
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Called {nameof(MoveOnPath)} but {nameof(PathId)} is null or empty and {nameof(CurrentPathPoint)} is null (NPC: {Owner})");

                    return;
                }

                CurrentPathPoint = MovementMgr.LoadPath(PathId);

                if (CurrentPathPoint == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Called {nameof(MoveOnPath)} but {nameof(MovementMgr.LoadPath)} returned null ({nameof(PathId)}: {PathId}) (NPC: {Owner})");

                    return;
                }

                SetFlag(MovementState.OnPath);
                PathTo(new(CurrentPathPoint.X, CurrentPathPoint.Y, CurrentPathPoint.Z), Math.Min(_moveOnPathSpeed, CurrentPathPoint.MaxSpeed));
                return;
            }

            if (IsFlagSet(MovementState.OnPath))
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Called {nameof(MoveOnPath)} but {nameof(MovementState.OnPath)} is already set (NPC: {Owner})");

                return;
            }

            SetFlag(MovementState.OnPath);

            // Ignore the first path point if it's very close.
            // If the distance is 0 and we don't skip it, WalkToInternal will stop any movement.
            if (Owner.IsWithinRadius(CurrentPathPoint, 10))
            {
                MoveToNextPathPoint();
                return;
            }

            CurrentPathPoint = _path.IsReversing ?
                CurrentPathPoint.Next ?? CurrentPathPoint :
                CurrentPathPoint.Prev ?? CurrentPathPoint;
            Vector3 destination = new(CurrentPathPoint.X, CurrentPathPoint.Y, CurrentPathPoint.Z);
            PathTo(destination, Owner.MaxSpeed);
        }

        public void StopMovingOnPath()
        {
            if (!IsFlagSet(MovementState.OnPath))
                return;

            UnsetFlag(MovementState.OnPath);

            if (Owner is GameTaxi or GameTaxiBoat)
                Owner.RemoveFromWorld();

            // We don't reset CurrentPathPoint here to allow the path to be resumed. This must be done manually if needed (on NPC death for example).
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
            PathTo(new(Owner.SpawnPoint.X, Owner.SpawnPoint.Y, Owner.SpawnPoint.Z), speed);
        }

        public void Roam(short speed)
        {
            // `CanRoam` returns false if `RoamingRange` is <= 0.
            if (!CanRoam)
                return;

            int maxRoamingRadius = Owner.RoamingRange;

            if (Owner.CurrentZone.IsPathfindingEnabled)
            {
                EDtPolyFlags[] filters = PathfindingProvider.Instance.DefaultFilters;
                Vector3? target = PathfindingProvider.Instance.GetRandomPoint(Owner.CurrentZone, new(Owner.SpawnPoint.X, Owner.SpawnPoint.Y, Owner.SpawnPoint.Z), maxRoamingRadius, filters);

                if (target.HasValue)
                    PathTo(target.Value, speed);

                return;
            }

            maxRoamingRadius = Util.Random(maxRoamingRadius);
            double angle = Util.RandomDouble() * Math.PI * 2;
            double targetX = Owner.SpawnPoint.X + maxRoamingRadius * Math.Cos(angle);
            double targetY = Owner.SpawnPoint.Y + maxRoamingRadius * Math.Sin(angle);
            WalkTo(new((float) targetX, (float) targetY, Owner.SpawnPoint.Z), speed);
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
            if (Owner.Heading == heading || Owner.IsCrowdControlled || IsTurningDisabled)
                return;

            if (duration > 0)
            {
                SetFlag(MovementState.TurnTo);

                if (_resetHeadingTimer == null)
                {
                    _resetHeadingTimer = CreateResetHeadingTimer();
                    _resetHeadingTimer.Start(duration);
                }
                else
                {
                    // Attempt to extend the duration of our existing `ResetHeadingAction`.
                    _resetHeadingTimer.Start(duration);

                    if (!_resetHeadingTimer.IsAlive)
                    {
                        _resetHeadingTimer = CreateResetHeadingTimer();
                        _resetHeadingTimer.Start(duration);
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
            // Toggle visualization for both `Pathfinder` (pathfinding) and `PathPoint` (patrols, horse routes).

            _pathfinder.ToggleVisualization();

            if (_pathVisualization != null)
            {
                _pathVisualization.CleanUp();
                _pathVisualization = null;
                return;
            }

            _pathVisualization = new();

            if (CurrentPathPoint != null)
                _pathVisualization.Visualize(MovementMgr.FindFirstPathPoint(CurrentPathPoint), Owner.CurrentRegion);
        }

        public override void ForceUpdatePosition()
        {
            base.ForceUpdatePosition();
            _positionForClient = _ownerPosition;
            _lastPositionUpdateTick = GameLoop.GameLoopTime;
        }

        protected override void RemoveFromServiceObjectStore()
        {
            base.RemoveFromServiceObjectStore();
            ClearAntiExploitImmunity();
        }

        private void UpdatePosition()
        {
            if (_lastPositionUpdateTick == GameLoop.GameLoopTime)
                return;

            if (!IsMoving)
            {
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

            float distSqr = (_destination - _ownerPosition).LengthSquared();
            float moveSqr = movementDelta.LengthSquared();

            // If the distance we are about to move is greater than the distance to the target, we have arrived.
            _ownerPosition = moveSqr >= distSqr ? _destination : potentialPosition;
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

            if (!IsMoving || distanceToTarget <= 0)
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
            ClearAntiExploitImmunity();

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

            float distanceToTarget = (_ownerPosition - destination).Length();

            if (distanceToTarget > 25)
                TurnTo((int) destination.X, (int) destination.Y);
            else if (!IsFlagSet(MovementState.Pathfinding))
                TurnTo(FollowTarget);

            if (distanceToTarget <= 0)
            {
                _ownerPosition = destination;

                if (CurrentSpeed > 0)
                    UpdateMovement(0);

                return;
            }

            // Only call UpdateMovement (which broadcasts to clients) if something actually changed.
            if (CurrentSpeed != speed || !IsDestinationValid || _destination != destination)
                UpdateMovement(destination, distanceToTarget, speed);

            SetFlag(MovementState.WalkTo);

            if (IsFlagSet(MovementState.Pathfinding) || (IsFlagSet(MovementState.OnPath) && CurrentPathPoint?.WaitTime == 0))
                distanceToTarget = Math.Max(0, distanceToTarget - NODE_REACHED_DISTANCE);

            _walkingToEstimatedArrivalTime = GameLoop.GameLoopTime + (long) (distanceToTarget * 1000 / speed);
        }

        private void PathToInternal(Vector3 destination, short speed)
        {
            if (!_pathfinder.ShouldPath)
            {
                FallbackToWalk(this, destination, speed);
                return;
            }

            // Don't substitute this with CurrentZone.
            Zone zone = Owner.CurrentRegion.GetZone((int) _ownerPosition.X, (int) _ownerPosition.Y);

            if (zone?.IsPathfindingEnabled != true)
            {
                FallbackToWalk(this, destination, speed);
                return;
            }

            PathingStep step = _pathfinder.GetNextStep(zone, _ownerPosition, destination);
            Vector3? snapPosition = step.SnapPosition;

            if (snapPosition.HasValue)
                _ownerPosition = snapPosition.GetValueOrDefault();

            if (step.Result is NextNodeResult.Valid)
            {
                _movementRequest.Set(MovementRequestType.Path, destination, speed);
                SetFlag(MovementState.Pathfinding);
                WalkToInternal(step.NextNode.GetValueOrDefault(), speed);
                return;
            }

            if (step.Result is NextNodeResult.Waiting)
            {
                PauseMovement(this, destination);
                return;
            }

            // End of path.
            switch (_pathfinder.PathfindingStatus)
            {
                case PathfindingStatus.PathFound:
                {
                    EDtPolyFlags[] filters = PathfindingProvider.Instance.BlockingDoorAvoidanceFilters;

                    // Finalize the path if we have direct LoS to the destination.
                    // This ensures that the NPC stays on the mesh, assuming it's on it to begin with.
                    // Use the most restrictive filters for now, since we don't know which ones were used.
                    if (PathfindingProvider.Instance.HasLineOfSight(zone, _ownerPosition, destination, filters))
                        FallbackToWalk(this, destination, speed);
                    else
                        PauseMovement(this, destination);

                    break;
                }
                case PathfindingStatus.PartialPathFound:
                case PathfindingStatus.BufferTooSmall:
                case PathfindingStatus.NoPathFound: // Happens when either the current position or the destination isn't on a mesh.
                {
                    HandleUnreachableDestination(destination);
                    break;
                }
                case PathfindingStatus.NotSet:
                case PathfindingStatus.NavmeshUnavailable:
                {
                    FallbackToWalk(this, destination, speed);
                    break;
                }
                default:
                {
                    PauseMovement(this, destination);
                    break;
                }
            }

            static void FallbackToWalk(NpcMovementComponent component, Vector3 destination, short speed)
            {
                component.UnsetFlag(MovementState.Pathfinding);
                component.WalkToInternal(destination, speed);
            }

            static void PauseMovement(NpcMovementComponent component, Vector3 destination)
            {
                component.TurnTo((int) destination.X, (int) destination.Y);
                component.UnsetFlag(MovementState.Pathfinding);

                if (component.IsMoving)
                    component.UpdateMovement(0);
            }
        }

        private int FollowTick()
        {
            // Stop moving if the NPC is casting or using ranged weapons.
            if (Owner.IsCasting || Owner.rangeAttackComponent.RangedAttackState is not eRangedAttackState.None)
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

            // Snap the destination to the mesh with a generous search distance.
            Zone zone = Owner.CurrentRegion.GetZone((int) targetPos.X, (int) targetPos.Y);

            if (zone.IsPathfindingEnabled)
            {
                const float MAX_SNAP_DISTANCE = 128f;

                if (!PathfindingProvider.Instance.TrySnapToMesh(zone, ref targetPos, MAX_SNAP_DISTANCE))
                {
                    HandleUnreachableDestination(targetPos);
                    return Properties.GAMENPC_FOLLOWCHECK_TIME;
                }
            }

            Vector3 relative = targetPos - _ownerPosition;
            float distanceSquared = relative.LengthSquared();

            if (distanceSquared > MaxFollowDistance * MaxFollowDistance)
            {
                StopFollowing();
                return 0;
            }

            // The way position is updated ensures that we never move past the destination, so we need to take potential small inaccuracies into account.
            if (distanceSquared <= (MinFollowDistance + 1) * (MinFollowDistance + 1))
            {
                ClearAntiExploitImmunity();
                TurnTo(FollowTarget);

                if (IsMoving)
                    UpdateMovement(0);

                UnsetFlag(MovementState.Pathfinding); // Ensures the NPC doesn't try to reach remaining nodes.
                return Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            float distance = MathF.Sqrt(distanceSquared);
            short speed;

            // No smoothing if the NPC is attacking and is out of melee range.
            if (Owner.IsAttacking && distance > Owner.MeleeAttackRange)
                speed = MaxSpeed;
            else
                speed = (short) Math.Min(MaxSpeed, (distance - MinFollowDistance) * 2.5);

            PathToInternal(targetPos, Math.Max((short) 20, speed));
            return Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        private void HandleUnreachableDestination(Vector3 destination)
        {
            // Non-pet NPCs are teleported to the closest reachable node from a reverse-path.
            // The teleport can cover a large distance in some cases, for example when both the NPC and the player are on a mesh island.
            // NPCs that are in combat and can't be teleported receive a damage immunity ability and start regenerating HP.
            // This helps against exploits and misplaced NPCs.

            // Pets following their owner are teleported at their feet if both are out of combat.
            // This allows them to keep up if they jump down a ledge or bridge.
            // This can theoretically be exploited by players in combat, but it requires both the pet and the owner to leave combat.

            if (Owner.Brain is not ControlledMobBrain petBrain)
            {
                if (JumpToClosestReachableNode(this, destination))
                    return;

                if (Owner.InCombat || Owner.Brain is StandardMobBrain { HasAggro: true })
                    StartAntiExploitImmunity();
            }
            else if (!Owner.InCombat &&
                !petBrain.Owner.InCombat &&
                FollowTarget != null &&
                petBrain.Owner == FollowTarget)
            {
                if (TeleportPetToFloorBeneathOwner(this, petBrain))
                    return;
            }

            TurnTo((int) destination.X, (int) destination.Y);
            UnsetFlag(MovementState.Pathfinding);

            if (IsMoving)
                UpdateMovement(0);

            static bool JumpToClosestReachableNode(NpcMovementComponent component, Vector3 destination)
            {
                Zone zone = component.Owner.CurrentRegion.GetZone((int) destination.X, (int) destination.Y);

                if (!component._pathfinder.TryGetClosestReachableNode(zone, destination, component._ownerPosition, out Vector3? node) || !node.HasValue)
                    return false;

                component._ownerPosition = node.Value;
                component.UpdateMovement(0);
                component._pathfinder.ForceReplot = true;
                return true;
            }

            static bool TeleportPetToFloorBeneathOwner(NpcMovementComponent component, ControlledMobBrain petBrain)
            {
                const int MAX_TELEPORT_TRIGGER_RANGE = 1024;
                const int MAX_FLOOR_SEARCH_DEPTH = 1024;
                const int MIN_TELEPORT_DISTANCE = 128;

                GamePlayer playerOwner = petBrain.GetPlayerOwner();

                if (!component.Owner.IsWithinRadius(playerOwner, MAX_TELEPORT_TRIGGER_RANGE))
                    return false;

                Vector3 playerOwnerPos = new(playerOwner.X, playerOwner.Y, playerOwner.Z);
                EDtPolyFlags[] filters = PathfindingProvider.Instance.DefaultFilters;
                Vector3? floor = PathfindingProvider.Instance.GetFloorBeneath(playerOwner.CurrentZone, playerOwnerPos, MAX_FLOOR_SEARCH_DEPTH, filters);

                if (!floor.HasValue || component.Owner.IsWithinRadius(floor.Value, MIN_TELEPORT_DISTANCE))
                    return false;

                component._ownerPosition = floor.Value;
                component.UpdateMovement(0);
                component._pathfinder.ForceReplot = true;
                return true;
            }
        }

        private void StartAntiExploitImmunity()
        {
            _antiExploitImmunity ??= new(this);
        }

        private void ClearAntiExploitImmunity()
        {
            if (_antiExploitImmunity?.IsAlive != true)
                return;

            _antiExploitImmunity.Stop();
            _antiExploitImmunity = null;
        }

        private void OnArrival()
        {
            if (IsFlagSet(MovementState.Pathfinding))
            {
                ProcessMovementRequest();

                if (IsFlagSet(MovementState.WalkTo))
                    return;
            }

            if (IsFlagSet(MovementState.Follow))
                return;

            if (IsFlagSet(MovementState.OnPath))
            {
                if (CurrentPathPoint != null && !Owner.Brain.OnPathPointReached(CurrentPathPoint))
                {
                    if (CurrentPathPoint.WaitTime == 0)
                    {
                        MoveToNextPathPoint();
                        return;
                    }

                    SetFlag(MovementState.AtPathPoint);
                    _stopAtPathPointUntil = GameLoop.GameLoopTime + CurrentPathPoint.WaitTime * 100;
                }
                else
                    StopMovingOnPath();
            }

            if (IsMoving)
            {
                _ownerPosition = _destination;
                UpdateMovement(0);
            }
        }

        private void MoveToNextPathPoint()
        {
            PathPoint next = _path.IsReversing ? CurrentPathPoint.Prev : CurrentPathPoint.Next;

            if (next == null)
            {
                switch (CurrentPathPoint.Type)
                {
                    case EPathType.Loop:
                    {
                        CurrentPathPoint = MovementMgr.FindFirstPathPoint(CurrentPathPoint);
                        break;
                    }
                    case EPathType.Once:
                    {
                        // Clear fully so CanMoveOnPath returns false and the brain doesn't restart.
                        _path = Path.None;
                        StopMovingOnPath();
                        return;
                    }
                    case EPathType.Path_Reverse:
                    {
                        _path.IsReversing = !_path.IsReversing;
                        next = _path.IsReversing ? CurrentPathPoint.Prev : CurrentPathPoint.Next;
                        _path.Point = next;
                        break;
                    }
                }
            }
            else
                _path.Point = next;

            if (_path.Point != null)
            {
                Vector3 destination = new(CurrentPathPoint.X, CurrentPathPoint.Y, CurrentPathPoint.Z);
                short speed = Math.Min(_moveOnPathSpeed, _path.Point.MaxSpeed);

                // Minor optimization allowing faster transitions if this is called from Tick.
                if (ServiceObjectId.IsRunning)
                    PathToInternal(destination, speed);
                else
                    PathTo(destination, speed);
            }
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

            float magic;
            float ratio;

            if (wasMoving)
                _positionForClient = _ownerPosition;
            else
            {
                magic = CurrentSpeed * 0.15f;
                ratio = (float) ((distanceToTarget + magic) / distanceToTarget);
                _positionForClient = Vector3.Lerp(_destination, _ownerPosition, ratio);
            }

            magic = Math.Max(15, CurrentSpeed * 0.15f);
            ratio = (float) ((distanceToTarget + magic) / distanceToTarget);
            _destinationForClient = Vector3.Lerp(_ownerPosition, _destination, ratio);
        }

        private void UpdateMovement(short speed)
        {
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

            IsDestinationValid = distanceToTarget >= 0;
            _destination = destination;
            _needsBroadcastUpdate = true;

            bool wasMoving = IsMoving;
            CurrentSpeed = speed;
            UpdateVelocity(distanceToTarget);
            PrepareValuesForClient(wasMoving, distanceToTarget);
        }

        private ResetHeadingTimer CreateResetHeadingTimer()
        {
            return new(this, () =>
            {
                UnsetFlag(MovementState.TurnTo);
                _resetHeadingTimer = null;
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

        private class ResetHeadingTimer : ECSGameTimerWrapperBase
        {
            private NpcMovementComponent _movementComponent;
            private ushort _oldHeading;
            private long _oldMovementStartTick;
            private Action _onCompletion;

            public ResetHeadingTimer(NpcMovementComponent movementComponent, Action onCompletion) : base(movementComponent.Owner)
            {
                _movementComponent = movementComponent;
                _oldHeading = _movementComponent.Owner.Heading;
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

        private class AntiExploitImmunityTimer : ECSGameTimerWrapperBase
        {
            const int INTERVAL = 1000;
            const int INITIAL_INTERVAL = 3000;
            const double PERCENT_PER_TICK = 0.05;

            private NpcMovementComponent _movementComponent;
            private GameNPC _owner;
            private bool _wasAbilityGiven;

            public AntiExploitImmunityTimer(NpcMovementComponent movementComponent) : base(movementComponent.Owner)
            {
                _movementComponent = movementComponent;
                _owner = _movementComponent.Owner;

                Start(INITIAL_INTERVAL);

                // Don't add the ability if the NPC is naturally immune to damage.
                if (_owner.HasAbility(Abilities.DamageImmunity))
                    return;

                _owner.AddAbility(SkillBase.GetAbility(Abilities.DamageImmunity));
                _wasAbilityGiven = true;
            }

            public new void Stop()
            {
                if (_wasAbilityGiven)
                    _owner.RemoveAbility(Abilities.DamageImmunity);

                base.Stop();
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                GameNPC owner = _movementComponent.Owner;
                owner.ChangeHealth(owner, eHealthChangeType.Regenerate, (int) Math.Ceiling(owner.MaxHealth * PERCENT_PER_TICK));
                return INTERVAL;
            }
        }

        [Flags]
        private enum MovementState
        {
            None = 0,
            Request = 1 << 1,     // Was requested to move.
            WalkTo = 1 << 2,      // Is moving and has a destination.
            Follow = 1 << 3,      // Is following an object.
            OnPath = 1 << 4,      // Is following a path / is patrolling.
            AtPathPoint = 1 << 5, // Is waiting at a path point.
            Pathfinding = 1 << 6, // Is moving using Pathfinder.
            TurnTo = 1 << 7       // Is facing a direction for a certain duration.
        }
    }
}
