using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using DOL.GS.Movement;
using DOL.Logging;
using static DOL.GS.GameNPC;

namespace DOL.GS
{
    public sealed class Pathfinder
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int NODE_REACHED_DISTANCE = 16; // Exposed for NpcMovementComponent.
        private const int NODE_REACHED_DISTANCE_STRICT = 2;
        private const int MIN_TARGET_DIFF_REPLOT_DISTANCE = 64;
        private const int DOOR_SEARCH_DISTANCE = 128;

        private PathBuffer _activePath = new();
        private PathBuffer _calculationBuffer = new();
        private Vector3 _lastTarget = Vector3.Zero;
        private PathVisualization _pathVisualization;
        private bool _allowedToPath;

        public GameNPC Owner { get; }
        public bool ForceReplot { get; set; }
        public PathfindingStatus PathfindingStatus { get; private set; }

        public static EDtPolyFlags[] DefaultFilters => PathfindingProvider.Instance.DefaultFilters;
        public static EDtPolyFlags[] BlockingDoorAvoidanceFilters => PathfindingProvider.Instance.BlockingDoorAvoidanceFilters;

        public Pathfinder(GameNPC owner)
        {
            ForceReplot = true;
            Owner = owner;
            _allowedToPath = Owner is not GameTaxi and not GameTaxiBoat;
        }

        public void Clear()
        {
            _activePath.Clear();
            _calculationBuffer.Clear();
            _lastTarget = Vector3.Zero;
            PathfindingStatus = PathfindingStatus.NotSet;
            ForceReplot = true;
        }

        public bool ShouldPath(Zone zone, Vector3 target)
        {
            if (zone == null || !zone.IsPathfindingEnabled)
                return false;

            if (!_allowedToPath)
                return false;

            if ((Owner.Flags & (eFlags.FLYING | eFlags.SWIMMING)) != 0)
                return false;

            // Target is in a different zone (TODO: implement this maybe? not sure if really required).
            if (Owner.CurrentRegion.GetZone((int) target.X, (int) target.Y) != zone)
                return false;

            return true;
        }

        private PathfindingStatus CalculatePath(PathBuffer pathBuffer, Zone zone, Vector3 position, Vector3 target, EDtPolyFlags[] filters)
        {
            const int MAX_PATH_NODES = 512;
            WrappedPathfindingNode[] rentedNodeBuffer = ArrayPool<WrappedPathfindingNode>.Shared.Rent(MAX_PATH_NODES);

            try
            {
                PathfindingResult pathfindingResult = PathfindingProvider.Instance.GetPathStraight(zone, position, target, filters, rentedNodeBuffer);

                pathBuffer.Clear();

                if (pathfindingResult.Status is PathfindingStatus.BufferTooSmall)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Path buffer for {Owner} was too small. Needed {pathfindingResult.NodeCount}, had {MAX_PATH_NODES}.");
                }

                int nodeCount = Math.Min(pathfindingResult.NodeCount, MAX_PATH_NODES);

                if (nodeCount > 0)
                {
                    // Keep track of doors on the path.
                    // Skip the first node if it isn't a door.
                    for (int i = 0; i < nodeCount; i++)
                    {
                        WrappedPathfindingNode node = rentedNodeBuffer[i];
                        bool isDoor = (node.Flags & EDtPolyFlags.AnyDoor) != 0;

                        if (i == 0 && !isDoor)
                            continue;

                        pathBuffer.Nodes.Enqueue(node);

                        if (!isDoor)
                            continue;

                        Point3D point = new(node.Position.X, node.Position.Y, node.Position.Z);
                        pathBuffer.Doors[node] = new(1);
                        Owner.CurrentRegion.GetInRadius(point, eGameObjectType.DOOR, DOOR_SEARCH_DISTANCE, pathBuffer.Doors[node]);
                    }
                }

                return pathfindingResult.Status;
            }
            finally
            {
                ArrayPool<WrappedPathfindingNode>.Shared.Return(rentedNodeBuffer);
            }
        }

        public NextNodeResult GetNextNode(Zone zone, Vector3 position, Vector3 target, out Vector3 nextNode)
        {
            ReplotIfNeeded(zone, position, target);

            // Check if any doors on the path have become closed and can't be opened via interaction.
            // If so, try to replot with door avoidance filters.
            if (PathContainsBlockingDoor())
                TryApplyAlternativePath(zone, position, target);

            if (!_activePath.Nodes.TryPeek(0, out WrappedPathfindingNode current))
            {
                nextNode = default;
                return NextNodeResult.PathComplete;
            }

            if (!Owner.IsWithinRadius(current.Position, NODE_REACHED_DISTANCE))
            {
                nextNode = current.Position;
                return NextNodeResult.Valid;
            }

            if (NodeContainsDoor(current, true))
            {
                nextNode = default;
                return NextNodeResult.Waiting;
            }

            int nodesToRemove = 0;
            int maxLookahead = Math.Min(_activePath.Nodes.Count, 6); // Limit lookahead in case this gets expensive.
            int furthestVisibleNodeIndex = -1;

            // Look ahead to find the furthest node we can walk straight to.
            // This stops at the first door, at any node we don't have LoS to, or at any node that would require a big jump in height compared to the current node.
            for (int i = 0; i < maxLookahead; i++)
            {
                WrappedPathfindingNode candidateNode = _activePath.Nodes.Peek(i);

                if (NodeContainsDoor(candidateNode, false))
                    break;

                if (!PathfindingProvider.Instance.HasLineOfSight(zone, position, candidateNode.Position, DefaultFilters))
                    break;

                if (!IsStraightLineHeightSafe(position, candidateNode.Position, i))
                    break;

                furthestVisibleNodeIndex = i;
            }

            if (furthestVisibleNodeIndex > 0)
                nodesToRemove = furthestVisibleNodeIndex;
            else if (Owner.IsWithinRadius(current.Position, NODE_REACHED_DISTANCE_STRICT))
                    nodesToRemove = 1;
            else if (Owner.movementComponent.IsMoving)
            {
                float dot = Vector3.Dot(current.Position - position, Vector3.Normalize(Owner.movementComponent.Velocity));

                if (dot < 0f)
                    nodesToRemove = 1;
            }

            for (int i = 0; i < nodesToRemove; i++)
            {
                WrappedPathfindingNode node = _activePath.Nodes.Dequeue();

                if (_activePath.Doors.Remove(node, out List<GameDoorBase> doors))
                {
                    foreach (GameDoorBase door in doors)
                    {
                        if (door.CanBeOpenedViaInteraction && door.State is not eDoorState.Open)
                            door.Open();
                    }
                }
            }

            if (!_activePath.Nodes.TryPeek(0, out WrappedPathfindingNode next))
            {
                nextNode = default;
                return NextNodeResult.PathComplete;
            }

            nextNode = next.Position;
            return NextNodeResult.Valid;
        }

        private bool IsStraightLineHeightSafe(Vector3 start, Vector3 target, int candidateIndex)
        {
            // This should be low enough so that we avoid putting the NPC in a position where it couldn't be snapped to the mesh anymore.
            const float MAX_SAFE_HEIGHT_DEVIATION = 32f;

            if (candidateIndex == 0)
                return true;

            float targetDx = target.X - start.X;
            float targetDy = target.Y - start.Y;
            float sqrTotalDistance2D = targetDx * targetDx + targetDy * targetDy;

            if (sqrTotalDistance2D <= 0f)
                return Math.Abs(start.Z - target.Z) <= MAX_SAFE_HEIGHT_DEVIATION;

            float invSqrTotalDistance2D = 1f / sqrTotalDistance2D;

            for (int i = 0; i < candidateIndex; i++)
            {
                Vector3 node = _activePath.Nodes.Peek(i).Position;
                float nodeDx = node.X - start.X;
                float nodeDy = node.Y - start.Y;

                // Project the intermediate node onto the line segment.
                // This calculates the 't' progression (0.0 to 1.0) along the segment.
                float t = (nodeDx * targetDx + nodeDy * targetDy) * invSqrTotalDistance2D;
                t = Math.Clamp(t, 0f, 1f);

                float expectedZ = float.Lerp(start.Z, target.Z, t);

                if (Math.Abs(expectedZ - node.Z) > MAX_SAFE_HEIGHT_DEVIATION)
                    return false;
            }

            return true;
        }

        private void ReplotIfNeeded(Zone zone, Vector3 position, Vector3 target)
        {
            // Check if we can reuse our path. We assume that we ourselves never "suddenly" warp to a completely different pos.
            if (!ForceReplot && _lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
                return;

            PathfindingStatus status = CalculatePath(_activePath, zone, position, target, DefaultFilters);
            UpdatePathState(status, target);
        }

        private void TryApplyAlternativePath(Zone zone, Vector3 position, Vector3 target)
        {
            PathfindingStatus altStatus = CalculatePath(_calculationBuffer, zone, position, target, BlockingDoorAvoidanceFilters);

            // Abort if the alternative path isn't complete and let the caller handle the original path.
            if (altStatus is not PathfindingStatus.PathFound)
                return;

            (_activePath, _calculationBuffer) = (_calculationBuffer, _activePath);
            UpdatePathState(altStatus, target);
        }

        private void UpdatePathState(PathfindingStatus status, Vector3 target)
        {
            PathfindingStatus = status;
            _lastTarget = target;
            ForceReplot = false;
            _pathVisualization?.Visualize(_activePath.Nodes, Owner.CurrentRegion);
        }

        private bool PathContainsBlockingDoor()
        {
            foreach (List<GameDoorBase> doorsAroundNode in _activePath.Doors.Values)
            {
                foreach (GameDoorBase door in doorsAroundNode)
                {
                    if (door.State is eDoorState.Closed && !door.CanBeOpenedViaInteraction)
                        return true;
                }
            }

            return false;
        }

        private bool NodeContainsDoor(WrappedPathfindingNode node, bool blockingOnly)
        {
            if (_activePath.Doors.TryGetValue(node, out List<GameDoorBase> doors))
            {
                foreach (GameDoorBase door in doors)
                {
                    if (!blockingOnly || (door.State is eDoorState.Closed && !door.CanBeOpenedViaInteraction))
                        return true;
                }
            }

            return false;
        }

        public bool TryGetClosestReachableNode(Zone zone, Vector3 position, Vector3 target, out Vector3? node)
        {
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
            {
                CalculatePath(_activePath, zone, position, target, BlockingDoorAvoidanceFilters);
                _lastTarget = target;
                ForceReplot = false;
            }

            if (_activePath.Nodes.Count <= 0)
            {
                node = null;
                return false;
            }

            node = _activePath.Nodes.Peek(_activePath.Nodes.Count - 1).Position;
            return true;
        }

        public void ToggleVisualization()
        {
            if (_pathVisualization != null)
            {
                _pathVisualization.CleanUp();
                _pathVisualization = null;
                return;
            }

            _pathVisualization = new();
        }

        public override string ToString()
        {
            return $"{nameof(Pathfinder)}[Target={_lastTarget}, " +
                $"Nodes={_activePath.Nodes.Count}, " +
                $"NextNode={(_activePath.Nodes.Count > 0 ? _activePath.Nodes.Peek(0).ToString() : null)}]";
        }

        public enum NextNodeResult
        {
            Valid,
            Waiting,
            PathComplete
        }

        private class PathBuffer
        {
            public RingQueue<WrappedPathfindingNode> Nodes { get; } = new();
            public Dictionary<WrappedPathfindingNode, List<GameDoorBase>> Doors { get; } = new();

            public void Clear()
            {
                Nodes.Clear();
                Doors.Clear();
            }
        }
    }
}
