using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using DOL.GS.Movement;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class Pathfinder
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int MIN_TARGET_DIFF_REPLOT_DISTANCE = 64;
        public const int NODE_REACHED_DISTANCE = 16;
        public const int NODE_REACHED_DISTANCE_STRICT = 2;
        public const int NODE_MAX_SKIP_DISTANCE = 80;
        public const int DOOR_SEARCH_DISTANCE = 128;

        public GameNPC Owner { get; }
        public bool ForceReplot { get; set; }
        public PathfindingStatus PathfindingStatus { get; private set; }

        private PathBuffer _activePath = new();
        private PathBuffer _calculationBuffer = new();

        private Vector3 _lastTarget = Vector3.Zero;
        private PathVisualization _pathVisualization;

        public static EDtPolyFlags[] DefaultFilters => PathfindingProvider.Instance.DefaultFilters;
        public static EDtPolyFlags[] BlockingDoorAvoidanceFilters => PathfindingProvider.Instance.BlockingDoorAvoidanceFilters;

        public Pathfinder(GameNPC owner)
        {
            ForceReplot = true;
            Owner = owner;
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
            if (Owner.Flags.HasFlag(GameNPC.eFlags.FLYING) || Owner is GameTaxi || Owner is GameTaxiBoat)
                return false;

            if (zone == null || !zone.IsPathfindingEnabled)
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
                        pathBuffer.Doors[node] = new();
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

        public bool TryGetNextNode(Zone zone, Vector3 position, Vector3 target, out Vector3? nextNode)
        {
            // Check if we can reuse our path. We assume that we ourselves never "suddenly" warp to a completely different pos.
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
            {
                PathfindingStatus status = CalculatePath(_activePath, zone, position, target, DefaultFilters);
                UpdatePathState(status, target);
            }

            // Check if any doors on the path have become closed and can't be opened via interaction.
            // If so, replot with door avoidance filters.
            if (PathContainClosedUnopenableDoor(_activePath.Doors) && !TryApplyAlternativePath(zone, position, target))
            {
                nextNode = null;
                return false;
            }

            // Dequeue the next node if we're close to it, and any subsequent node that might be close.
            // Prevent corner-cutting by raycasting to the next node before removing the current one.
            // Open any doors associated with the node as we reach it.
            ManagePathProgress(zone, position);

            if (_activePath.Nodes.Count == 0)
            {
                nextNode = null;
                return false;
            }

            nextNode = _activePath.Nodes.Peek(0).Position;
            return true;
        }

        private static bool PathContainClosedUnopenableDoor(Dictionary<WrappedPathfindingNode, List<GameDoorBase>> doorsOnPath)
        {
            foreach (List<GameDoorBase> doorsAroundNode in doorsOnPath.Values)
            {
                foreach (GameDoorBase door in doorsAroundNode)
                {
                    if (door.State is eDoorState.Closed && !door.CanBeOpenedViaInteraction)
                        return true;
                }
            }

            return false;
        }

        private bool TryApplyAlternativePath(Zone zone, Vector3 position, Vector3 target)
        {
            PathfindingStatus altStatus = CalculatePath(_calculationBuffer, zone, position, target, BlockingDoorAvoidanceFilters);

            // Stop here if the alternative path isn't complete.
            // This prevents NPCs from trying to get closer to their target by walking to the other side of a keep.
            // Basically, if the primary path contains an impassable doors, take the alternative path unless it isn't complete.
            if (altStatus is PathfindingStatus.PathFound)
            {
                (_activePath, _calculationBuffer) = (_calculationBuffer, _activePath);
                UpdatePathState(altStatus, target);
                return true;
            }

            PathfindingStatus = PathfindingStatus.NoPathFound;
            return false;
        }

        private void UpdatePathState(PathfindingStatus status, Vector3 target)
        {
            PathfindingStatus = status;
            _lastTarget = target;
            ForceReplot = false;
            _pathVisualization?.Visualize(_activePath.Nodes, Owner.CurrentRegion);
        }

        private void ManagePathProgress(Zone zone, Vector3 position)
        {
            if (!_activePath.Nodes.TryPeek(0, out WrappedPathfindingNode current) || !Owner.IsWithinRadius(current.Position, NODE_REACHED_DISTANCE))
                return;

            int nodesToRemove = 0;
            int count = _activePath.Nodes.Count;

            for (int i = 1; i < count; i++)
            {
                Vector3 candidatePosition = _activePath.Nodes.Peek(i).Position;

                if (!Owner.IsWithinRadius(candidatePosition, NODE_MAX_SKIP_DISTANCE))
                    break;

                if (!PathfindingProvider.Instance.HasLineOfSight(zone, position, candidatePosition, DefaultFilters))
                    break;

                nodesToRemove = i;
            }

            // If we don't have LoS to any subsequent node, only remove the current one if we're really close, or if we're moving away from it.
            if (nodesToRemove == 0)
            {
                if (Owner.IsWithinRadius(current.Position, NODE_REACHED_DISTANCE_STRICT))
                    nodesToRemove = 1;
                else if (Owner.movementComponent.IsMoving)
                {
                    float dot = Vector3.Dot(current.Position - position, Vector3.Normalize(Owner.movementComponent.Velocity));

                    if (dot < 0f)
                        nodesToRemove = 1;
                }
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
        }

        public bool TryGetClosestReachableNode(Zone zone, Vector3 position, Vector3 target, out Vector3? node)
        {
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
            {
                CalculatePath(_activePath, zone, position, target, DefaultFilters);
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
