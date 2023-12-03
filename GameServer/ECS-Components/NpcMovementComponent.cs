using System;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;
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

        private MovementType _movementType;
        private long _nextFollowTick;
        private int _followTickInterval;
        private long _walkingToEstimatedArrivalTime;
        private short _moveOnPathSpeed;
        private long _stopAtWaypointUntil;
        private PathCalculator _pathCalculator;
        private Action<NpcMovementComponent> _goToNextPathingNodeCallback;

        public new GameNPC Owner { get; private set; }
        // 'TargetPosition' is accessed from multiple threads simultaneously (from the current NPC being updated, others NPCs checking around them, and the world update thread).
        // Actual synchronization would be expensive, so instead threads are expected to check 'IsTargetPositionValid' before using it, which is set to false when a NPC stops.
        // This however means 'TargetPosition' might be slightly outdated.
        public IPoint3D TargetPosition { get; private set; }
        public GameObject FollowTarget { get; private set; }
        public int FollowMinDistance { get; private set; } = 100;
        public int FollowMaxDistance { get; private set; } = 3000;
        public string PathID { get; set; }
        public PathPoint CurrentWaypoint { get; set; }
        public bool IsReturningToSpawnPoint { get; private set; }
        public bool IsTargetPositionValid { get; private set; }
        public int RoamingRange { get; set; }
        public bool IsMovingOnPath => IsSet(MovementType.ON_PATH);
        public bool IsNearSpawn => Owner.IsWithinRadius(Owner.SpawnPoint, 25);
        public bool IsAtTargetPosition => IsTargetPositionValid && TargetPosition.X == Owner.X && TargetPosition.Y == Owner.Y && TargetPosition.Z == Owner.Z;
        public bool CanRoam => ServerProperties.Properties.ALLOW_ROAM && RoamingRange != 0 && string.IsNullOrWhiteSpace(PathID);

        public NpcMovementComponent(GameNPC npcOwner) : base(npcOwner)
        {
            Owner = npcOwner;
            _pathCalculator = new(npcOwner);
        }

        public override void Tick()
        {
            if (IsSet(MovementType.FOLLOW))
            {
                if (ServiceUtils.ShouldTickAdjust(ref _nextFollowTick))
                {
                    _followTickInterval = FollowTick();

                    if (_followTickInterval != 0)
                        _nextFollowTick += _followTickInterval;
                    else
                        _movementType &= ~MovementType.FOLLOW;
                }
            }

            if (IsSet(MovementType.WALK_TO))
            {
                if (ServiceUtils.ShouldTick(_walkingToEstimatedArrivalTime))
                {
                    _movementType &= ~MovementType.WALK_TO;
                    OnArrival();
                }
            }

            if (IsSet(MovementType.AT_WAYPOINT))
            {
                if (ServiceUtils.ShouldTick(_stopAtWaypointUntil))
                {
                    _movementType &= ~MovementType.AT_WAYPOINT;
                    MoveToNextWaypoint();
                }
            }

            base.Tick();
        }

        public void WalkTo(IPoint3D targetPosition, short speed)
        {
            if (IsTurningDisabled)
                return;

            if (speed > MaxSpeed)
                speed = MaxSpeed;

            if (speed <= 0)
                return;

            if (targetPosition == null)
            {
                UpdateMovement(null, speed);
                return;
            }

            int distanceToTarget = Owner.GetDistanceTo(targetPosition);
            int ticksToArrive = distanceToTarget * 1000 / speed;

            if (ticksToArrive > 0)
            {
                UpdateMovement(targetPosition, speed);

                if (distanceToTarget > 25)
                    TurnTo(targetPosition.X, targetPosition.Y);

                _movementType |= MovementType.WALK_TO;
                _walkingToEstimatedArrivalTime = GameLoop.GameLoopTime + ticksToArrive;
            }
            else
                OnArrival();
        }

        public bool PathTo(IPoint3D targetPosition, short speed)
        {
            // Not optimal but we don't want to use the object directly because its position is likely to change.
            // This would break 'PathToInternal'.
            targetPosition = new Point3D(targetPosition.X, targetPosition.Y, targetPosition.Z);
            return PathToInternal(targetPosition, speed);
        }

        public void StopMoving()
        {
            _movementType = MovementType.NONE;
            StopFollowing();
            StopMovingOnPath();
            CancelReturnToSpawnPoint();
            UpdateMovement(null, 0);
        }

        public void Follow(GameObject target, int minDistance, int maxDistance)
        {
            if (target == null || target.ObjectState != eObjectState.Active)
                return;

            FollowTarget = target;
            FollowMinDistance = minDistance;
            FollowMaxDistance = maxDistance;
            _movementType |= MovementType.FOLLOW;
        }

        public void StopFollowing()
        {
            FollowTarget = null;
            _movementType &= ~MovementType.FOLLOW;
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
                    log.Error($"Called MoveOnPath but PathID is null (NPC: {Owner})");
                    return;
                }

                CurrentWaypoint = MovementMgr.LoadPath(PathID);

                if (CurrentWaypoint == null)
                {
                    log.Error($"Called MoveOnPath but LoadPath returned null (PathID: {PathID}) (NPC: {Owner})");
                    return;
                }

                _movementType |= MovementType.ON_PATH;
                PathTo(CurrentWaypoint, Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
                return;
            }
            else if (!IsSet(MovementType.ON_PATH))
            {
                _movementType |= MovementType.ON_PATH;

                if (Owner.IsWithinRadius(CurrentWaypoint, 25))
                {
                    MoveToNextWaypoint();
                    return;
                }

                if (CurrentWaypoint.Type == EPathType.Path_Reverse && CurrentWaypoint.FiredFlag)
                    CurrentWaypoint = CurrentWaypoint.Next;
                else if (CurrentWaypoint.Prev != null)
                    CurrentWaypoint = CurrentWaypoint.Prev;

                PathTo(CurrentWaypoint, Owner.MaxSpeed);
            }
            else
                log.Error($"Called MoveOnPath but both CurrentWaypoint and ON_PATH are already set. (NPC: {Owner})");
        }

        public void StopMovingOnPath()
        {
            // Without this, horses would be immediately removed since 'MoveOnPath' immediately calls 'StopMoving', which calls 'StopMovingOnPath'.
            if (IsSet(MovementType.ON_PATH))
            {
                if (Owner is GameTaxi or GameTaxiBoat)
                {
                    UpdateMovement(null, 0);
                    Owner.RemoveFromWorld();
                }

                _movementType &= ~MovementType.ON_PATH;
            }
        }

        public void ReturnToSpawnPoint()
        {
            ReturnToSpawnPoint(DEFAULT_WALK_SPEED);
        }

        public void ReturnToSpawnPoint(short speed)
        {
            StopFollowing();
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
            int maxRoamingRadius = Owner.RoamingRange > 0 ? Owner.RoamingRange : Owner.CurrentRegion.IsDungeon ? 5 : 500;

            if (Owner.CurrentZone.IsPathingEnabled)
            {
                Vector3? target = PathingMgr.Instance.GetRandomPointAsync(Owner.CurrentZone, new Vector3(Owner.X, Owner.Y, Owner.Z), maxRoamingRadius);

                if (target.HasValue)
                    PathTo(new Point3D(target.Value.X, target.Value.Y, target.Value.Z), speed);

                return;
            }

            double targetX = Owner.SpawnPoint.X + Util.Random(-maxRoamingRadius, maxRoamingRadius);
            double targetY = Owner.SpawnPoint.Y + Util.Random(-maxRoamingRadius, maxRoamingRadius);
            WalkTo(new Point3D((int) targetX, (int) targetY, Owner.SpawnPoint.Z), speed);
        }

        protected override void UpdateTickSpeed()
        {
            Owner.NeedsBroadcastUpdate = true;

            if (IsTargetPositionValid)
            {
                double dist = Owner.GetDistanceTo(TargetPosition);

                if (dist <= 0)
                {
                    SetTickSpeed(0.0, 0.0, 0.0);
                    return;
                }

                double tickSpeed = CurrentSpeed * 0.001;
                double x = (TargetPosition.X - Owner.RealX) / dist;
                double y = (TargetPosition.Y - Owner.RealY) / dist;
                double z = (TargetPosition.Z - Owner.RealZ) / dist;
                SetTickSpeed(x * tickSpeed, y * tickSpeed, z * tickSpeed);
                return;
            }

            base.UpdateTickSpeed();
        }

        private bool PathToInternal(IPoint3D targetPosition, short speed)
        {
            // Pathing with no target position isn't currently supported.
            if (_pathCalculator == null || targetPosition == null)
            {
                _movementType &= ~MovementType.PATHING;
                WalkTo(targetPosition, speed);
                return false;
            }

            Vector3 dest = new(targetPosition.X, targetPosition.Y, targetPosition.Z);

            if (!PathCalculator.ShouldPath(Owner, dest))
            {
                _movementType &= ~MovementType.PATHING;
                WalkTo(targetPosition, speed);
                return false;
            }

            Tuple<Vector3?, NoPathReason> res = _pathCalculator.CalculateNextTarget(dest);
            Vector3? nextNode = res.Item1;
            //NoPathReason noPathReason = res.Item2;
            //bool shouldUseAirPath = noPathReason == NoPathReason.RECAST_FOUND_NO_PATH;
            //bool didFindPath = PathCalculator.DidFindPath;

            if (!nextNode.HasValue)
            {
                _movementType &= ~MovementType.PATHING;
                WalkTo(targetPosition, speed);
                return false;
            }

            // Do the actual pathing bit: Walk towards the next pathing node
            _movementType |= MovementType.PATHING;
            _goToNextPathingNodeCallback = x => x.PathToInternal(targetPosition, speed);
            WalkTo(new Point3D(nextNode.Value.X, nextNode.Value.Y, nextNode.Value.Z), speed);
            return true;
        }

        private int FollowTick()
        {
            // Stop moving if the NPC is casting or attacking with a ranged weapon.
            if (Owner.IsCasting || (Owner.attackComponent.IsAttacking && Owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance))
            {
                TurnTo(FollowTarget);

                if (IsMoving)
                    StopMoving();

                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            GameLiving followLiving = FollowTarget as GameLiving;

            if (followLiving.IsAlive == false || FollowTarget.ObjectState != eObjectState.Active || Owner.CurrentRegionID != FollowTarget.CurrentRegionID)
            {
                StopFollowing();
                return 0;
            }

            double diffX = (long) FollowTarget.X - Owner.X;
            double diffY = (long) FollowTarget.Y - Owner.Y;
            double diffZ = (long) FollowTarget.Z - Owner.Z;
            double distance = Math.Sqrt(diffX * diffX + diffY * diffY + diffZ * diffZ);

            // If distance is greater then the max follow distance, stop following and return home.
            if (distance > FollowMaxDistance)
            {
                ReturnToSpawnPoint();
                return 0;
            }

            Point3D targetPosition;

            if (Owner.Brain is StandardMobBrain brain)
            {
                // If the npc hasn't hit or been hit in a while, stop following and return home.
                if (brain is not IControlledBrain)
                {
                    if (Owner.attackComponent.AttackState && followLiving != null)
                    {
                        long duration = 25000 + brain.GetAggroAmountForLiving(followLiving) / (Owner.MaxHealth + 1) * 100;
                        long lastAttackTick = Owner.LastAttackTick;
                        long lastAttackedTick = Owner.LastAttackedByEnemyTick;

                        if (GameLoop.GameLoopTime - lastAttackTick > duration &&
                            GameLoop.GameLoopTime - lastAttackedTick > duration &&
                            lastAttackedTick != 0)
                        {
                            Owner.LastAttackedByEnemyTickPvE = 0;
                            Owner.LastAttackedByEnemyTickPvP = 0;
                            brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                            return 0;
                        }
                    }
                }

                // If we're part of a formation, we can get out early.
                if (Owner.FollowTarget.Realm == Owner.Realm)
                {
                    int newX = FollowTarget.X;
                    int newY = FollowTarget.Y;
                    int newZ = FollowTarget.Z;

                    if (brain.CheckFormation(ref newX, ref newY, ref newZ))
                    {
                        targetPosition = new(newX, newY, newZ);
                        double followSpeed = Math.Max(Math.Min(MaxSpeed, Owner.GetDistance(targetPosition) * FOLLOW_SPEED_SCALAR), 50);
                        PathTo(targetPosition, (short) followSpeed);
                        return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
                    }
                }
            }

            // Distances under 100 do not calculate correctly leading to the mob always being told to walkto.
            // Pets can follow closer. Need to implement /fdistance command to make this adjustable.
            int minAllowedFollowDistance = Owner.Brain is IControlledBrain ? MIN_ALLOWED_PET_FOLLOW_DISTANCE : MIN_ALLOWED_FOLLOW_DISTANCE;

            if (FollowMinDistance > minAllowedFollowDistance)
                minAllowedFollowDistance = FollowMinDistance;

            if (distance <= minAllowedFollowDistance)
            {
                if (IsMoving)
                    UpdateMovement(null, 0);

                TurnTo(FollowTarget);
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            diffX = diffX / distance * minAllowedFollowDistance;
            diffY = diffY / distance * minAllowedFollowDistance;
            diffZ = diffZ / distance * minAllowedFollowDistance;

            targetPosition = new((int) (FollowTarget.X - diffX), (int) (FollowTarget.Y - diffY), (int) (FollowTarget.Z - diffZ));

            // Slow down out of combat pets when they're close.
            if (!Owner.InCombat && Owner.Brain is ControlledNpcBrain controlledBrain && controlledBrain.Owner == Owner.FollowTarget)
                PathTo(targetPosition, (short) Math.Max(Math.Min(MaxSpeed, Owner.GetDistance(targetPosition) * FOLLOW_SPEED_SCALAR), 50));
            else
                PathTo(targetPosition, MaxSpeed);

            return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        private void OnArrival()
        {
            if (IsMoving)
                UpdateMovement(null, 0);

            if (IsSet(MovementType.PATHING))
            {
                _goToNextPathingNodeCallback(this);
                return;
            }

            if (IsReturningToSpawnPoint)
            {
                CancelReturnToSpawnPoint();
                TurnTo(Owner.SpawnHeading);
                return;
            }

            if (IsSet(MovementType.ON_PATH))
            {
                if (CurrentWaypoint != null)
                {
                    if (CurrentWaypoint.WaitTime == 0)
                        MoveToNextWaypoint();
                    else
                    {
                        _movementType |= MovementType.AT_WAYPOINT;
                        _stopAtWaypointUntil = GameLoop.GameLoopTime + CurrentWaypoint.WaitTime * 100;
                    }
                }
                else
                    StopMovingOnPath();
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
                        if (oldPathPoint.FiredFlag)
                            CurrentWaypoint = CurrentWaypoint.Next;
                        else
                            CurrentWaypoint = CurrentWaypoint.Prev;

                        break;
                    }
                }
            }
            else
            {
                if (CurrentWaypoint.Type == EPathType.Path_Reverse && CurrentWaypoint.FiredFlag)
                    CurrentWaypoint = CurrentWaypoint.Prev;
                else
                    CurrentWaypoint = CurrentWaypoint.Next;
            }

            oldPathPoint.FiredFlag = !oldPathPoint.FiredFlag;

            if (CurrentWaypoint != null)
                WalkTo(CurrentWaypoint, Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
            else
                StopMovingOnPath();
        }

        private void UpdateMovement(IPoint3D targetPosition, short speed)
        {
            Owner.X = Owner.X;
            Owner.Y = Owner.Y;
            Owner.Z = Owner.Z;
            MovementStartTick = GameLoop.GameLoopTime;

            if (targetPosition == null)
                IsTargetPositionValid = false;
            else
            {
                IsTargetPositionValid = true;
                TargetPosition = targetPosition;
            }

            CurrentSpeed = speed;
            UpdateTickSpeed();
        }

        private bool IsSet(MovementType flag)
        {
            return (_movementType & flag) == flag;
        }

        [Flags]
        private enum MovementType
        {
            NONE = 0,
            FOLLOW = 1,      // Is following an object.
            WALK_TO = 2,     // Has a destination.
            ON_PATH = 4,     // Following a path / patrolling.
            AT_WAYPOINT = 8, // Waiting at a waypoint.
            PATHING = 16     // Pathing is enabled.
        }
    }
}
