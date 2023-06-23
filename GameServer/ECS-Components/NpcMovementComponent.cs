using System;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;
using static DOL.GS.GameNPC;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcMovementComponent : MovementComponent
    {
        private const int MIN_ALLOWED_FOLLOW_DISTANCE = 100;
        private const int MIN_ALLOWED_PET_FOLLOW_DISTANCE = 90;
        private const short DEFAULT_WALK_TO_SPAWN_POINT_SPEED = 50;
        private const double FOLLOW_SPEED_SCALAR = 2.5;

        private MovementType _movementType;
        private long _followLastTick;
        private int _followTickInterval;
        private long _walkingToEstimatedArrivalTime;
        private short _moveOnPathMinSpeed;
        private long _stopAtWaypointUntil;
        private PathCalculator _pathCalculator;
        private Action<NpcMovementComponent> _goToNextPathingNodeCallback;

        public new GameNPC Owner { get; private set; }
        // 'TargetPosition' is accessed from multiple threads simultaneously (from the current NPC being updated, others NPCs checking around them, and the world update thread).
        // Actual synchronization would be expensive, so instead threads are expected to check 'IsTargetPositionValid' before using it, which is set to false when a NPC stops.
        // This however means 'TargetPosition' might be slightly outdated.
        public IPoint3D TargetPosition { get; private set; }
        public GameObject FollowTarget { get; private set; }
        public int FollowMaxDist { get; private set; } = 3000;
        public int FollowMinDist { get; private set; } = 100;
        public string PathID { get; set; }
        public PathPoint CurrentWaypoint { get; set; }
        public bool IsReturningHome { get; private set; }
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

        public override void Tick(long tick)
        {
            if (IsSet(MovementType.FOLLOW))
            {
                if (_followLastTick + _followTickInterval < tick)
                {
                    _followTickInterval = FollowTick();
                    _followLastTick = tick;

                    if (_followTickInterval == 0)
                        _movementType &= ~MovementType.FOLLOW;
                }
            }

            if (IsSet(MovementType.WALK_TO))
            {
                if (_walkingToEstimatedArrivalTime <= tick)
                {
                    _movementType &= ~MovementType.WALK_TO;
                    OnArrival();
                }
            }

            if (IsSet(MovementType.AT_WAYPOINT))
            {
                if (_stopAtWaypointUntil <= tick)
                {
                    _movementType &= ~MovementType.AT_WAYPOINT;
                    ResumePath();
                }
            }
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

            int ticksToArrive = Owner.GetDistanceTo(targetPosition) * 1000 / speed;

            if (ticksToArrive > 0)
            {
                UpdateMovement(targetPosition, speed);
                TurnTo(targetPosition.X, targetPosition.Y);

                // Cancel the ranged attack if the NPC is moving.
                if (Owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    Owner.StopAttack();
                    Owner.attackComponent.attackAction?.CleanUp();
                }

                _movementType |= MovementType.WALK_TO;
                _walkingToEstimatedArrivalTime = GameLoop.GameLoopTime + ticksToArrive;
            }
        }

        public bool PathTo(IPoint3D targetPosition, short speed)
        {
            // Pathing with no target position isn't currently supported.
            if (targetPosition == null)
            {
                _movementType &= ~MovementType.PATHING;
                WalkTo(targetPosition, speed);
                return false;
            }

            Vector3 dest = new(targetPosition.X, targetPosition.Y, targetPosition.Z);

            if (_pathCalculator == null || !PathCalculator.ShouldPath(Owner, dest))
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
            _goToNextPathingNodeCallback = x => x.PathTo(targetPosition, speed);
            WalkTo(new Point3D(nextNode.Value.X, nextNode.Value.Y, nextNode.Value.Z), speed);
            return true;
        }

        public void PathOrWalkTo(IPoint3D targetPosition, short speed)
        {
            if (Owner.CurrentZone.IsPathingEnabled)
                PathTo(targetPosition, speed);
            else
                WalkTo(targetPosition, speed);
        }

        public void StopMoving()
        {
            _movementType = MovementType.NONE;
            StopFollowing();
            StopMovingOnPath();
            CancelReturnToSpawnPoint();
            UpdateMovement(null, 0);
        }

        public void Follow(GameObject target)
        {
            Follow(target, FollowMinDist, FollowMaxDist);
        }

        public void Follow(GameObject target, int minDistance, int maxDistance)
        {
            if (target == null || target.ObjectState != eObjectState.Active)
                return;

            FollowTarget = target;
            FollowMaxDist = maxDistance;
            FollowMinDist = minDistance;
            _movementType |= MovementType.FOLLOW;
        }

        public void StopFollowing()
        {
            FollowTarget = null;
            _movementType &= ~MovementType.FOLLOW;
        }

        public void MoveOnPath(short minSpeed)
        {
            if (CurrentWaypoint == null)
            {
                if (log.IsWarnEnabled)
                    log.Error("No path to travel on for " + Owner.Name);

                return;
            }

            _moveOnPathMinSpeed = minSpeed;

            if (Owner.IsWithinRadius(CurrentWaypoint, 100))
            {
                Owner.FireAmbientSentence(eAmbientTrigger.moving, Owner);

                if (CurrentWaypoint.Type == ePathType.Path_Reverse && CurrentWaypoint.FiredFlag)
                    CurrentWaypoint = CurrentWaypoint.Prev;
                else
                {
                    if ((CurrentWaypoint.Type == ePathType.Loop) && (CurrentWaypoint.Next == null))
                        CurrentWaypoint = MovementMgr.FindFirstPathPoint(CurrentWaypoint);
                    else
                        CurrentWaypoint = CurrentWaypoint.Next;
                }
            }

            if (CurrentWaypoint != null)
            {
                _movementType |= MovementType.ON_PATH;
                PathOrWalkTo(CurrentWaypoint, Math.Min(_moveOnPathMinSpeed, CurrentWaypoint.MaxSpeed));
            }
            else
                StopMovingOnPath();
        }

        public void StopMovingOnPath()
        {
            _movementType &= ~MovementType.ON_PATH;

            if (Owner is GameTaxi or GameTaxiBoat)
            {
                StopMoving();
                Owner.RemoveFromWorld();
            }
        }

        public void ReturnToSpawnPoint()
        {
            ReturnToSpawnPoint(DEFAULT_WALK_TO_SPAWN_POINT_SPEED);
        }

        public void ReturnToSpawnPoint(short speed)
        {
            StopFollowing();
            Owner.TargetObject = null;
            Owner.attackComponent.StopAttack();
            (Owner.Brain as StandardMobBrain)?.ClearAggroList();
            IsReturningHome = true;
            IsReturningToSpawnPoint = true;
            PathOrWalkTo(Owner.SpawnPoint, speed);
        }

        public void CancelReturnToSpawnPoint()
        {
            IsReturningHome = false;
            IsReturningToSpawnPoint = false;
        }

        public void Roam(short speed)
        {
            int maxRoamingRadius = Owner.RoamingRange > 0 ? Owner.RoamingRange : Owner.CurrentRegion.IsDungeon ? 5 : 500;

            if (Owner.CurrentZone.IsPathingEnabled)
            {
                Vector3? target = PathingMgr.Instance.GetRandomPointAsync(Owner.CurrentZone, new Vector3(Owner.X, Owner.Y, Owner.Z), maxRoamingRadius);

                if (target.HasValue)
                    PathOrWalkTo(new Point3D(target.Value.X, target.Value.Y, target.Value.Z), speed);
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

        private int FollowTick()
        {
            if (Owner.IsCasting)
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;

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
            if (distance > FollowMaxDist)
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
                        PathOrWalkTo(targetPosition, (short) followSpeed);
                        return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
                    }
                }
            }

            // Distances under 100 do not calculate correctly leading to the mob always being told to walkto.
            // Pets can follow closer. Need to implement /fdistance command to make this adjustable.
            int minAllowedFollowDistance = Owner.Brain is IControlledBrain ? MIN_ALLOWED_PET_FOLLOW_DISTANCE : MIN_ALLOWED_FOLLOW_DISTANCE;

            if (FollowMinDist > minAllowedFollowDistance)
                minAllowedFollowDistance = FollowMinDist;

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
            if (!Owner.InCombat && Owner.Brain is ControlledNpcBrain controledBrain && controledBrain.Owner == Owner.FollowTarget)
                PathOrWalkTo(targetPosition, (short) Math.Max(Math.Min(MaxSpeed, Owner.GetDistance(targetPosition) * FOLLOW_SPEED_SCALAR), 50));
            else
                PathOrWalkTo(targetPosition, MaxSpeed);

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
                        ResumePath();
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

        private void ResumePath()
        {
            PathPoint oldPathPoint = CurrentWaypoint;
            PathPoint nextPathPoint = CurrentWaypoint.Next;

            if ((CurrentWaypoint.Type == ePathType.Path_Reverse) && CurrentWaypoint.FiredFlag)
                nextPathPoint = CurrentWaypoint.Prev;

            if (nextPathPoint == null)
            {
                switch (CurrentWaypoint.Type)
                {
                    case ePathType.Loop:
                    {
                        CurrentWaypoint = MovementMgr.FindFirstPathPoint(CurrentWaypoint);
                        break;
                    }
                    case ePathType.Once:
                    {
                        CurrentWaypoint = null;
                        break;
                    }
                    case ePathType.Path_Reverse:
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
                if ((CurrentWaypoint.Type == ePathType.Path_Reverse) && CurrentWaypoint.FiredFlag)
                    CurrentWaypoint = CurrentWaypoint.Prev;
                else
                    CurrentWaypoint = CurrentWaypoint.Next;
            }

            oldPathPoint.FiredFlag = !oldPathPoint.FiredFlag;

            if (CurrentWaypoint != null)
                PathOrWalkTo(CurrentWaypoint, Math.Min(_moveOnPathMinSpeed, CurrentWaypoint.MaxSpeed));
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
