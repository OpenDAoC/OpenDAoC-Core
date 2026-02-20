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
        public const int DOOR_SEARCH_DISTANCE = 64;

        public GameNPC Owner { get; }
        public bool ForceReplot { get; set; }
        public PathfindingStatus PathfindingStatus { get; private set; }

        private RingQueue<WrappedPathfindingNode> _pathNodes = new();
        private Dictionary<WrappedPathfindingNode, List<GameDoorBase>> _doorsOnPath = new();
        private Vector3 _lastTarget = Vector3.Zero;
        private PathVisualization _pathVisualization;

        public Pathfinder(GameNPC owner)
        {
            ForceReplot = true;
            Owner = owner;
        }

        public void Clear()
        {
            _pathNodes.Clear();
            _doorsOnPath.Clear();
            _lastTarget = Vector3.Zero;
            PathfindingStatus = PathfindingStatus.NotSet;
            ForceReplot = true;
        }

        public bool ShouldPath(Zone zone,Vector3 target)
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

        private void ReplotPath(Zone zone, Vector3 position, Vector3 target)
        {
            const int MAX_PATH_NODES = 512;
            WrappedPathfindingNode[] rentedNodeBuffer = ArrayPool<WrappedPathfindingNode>.Shared.Rent(MAX_PATH_NODES);

            try
            {
                PathfindingResult pathfindingResult = PathfindingProvider.Instance.GetPathStraight(zone, position, target, rentedNodeBuffer);

                _pathNodes.Clear();
                _doorsOnPath.Clear();

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
                        bool isDoor = (node.Flags & EDtPolyFlags.DOOR) != 0;

                        if (i == 0 && !isDoor)
                            continue;

                        _pathNodes.Enqueue(node);

                        if (!isDoor)
                            continue;

                        Point3D point = new(node.Position.X, node.Position.Y, node.Position.Z);
                        _doorsOnPath[node] = new();
                        Owner.CurrentRegion.GetInRadius(point, eGameObjectType.DOOR, DOOR_SEARCH_DISTANCE, _doorsOnPath[node]);
                    }
                }

                _pathVisualization?.Visualize(_pathNodes, Owner.CurrentRegion);
                _lastTarget = target;
                PathfindingStatus = pathfindingResult.Status;
                ForceReplot = false;
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
                ReplotPath(zone, position, target);

            // Stop right there if the path contains a closed door that we're not allowed to open.
            // We don't want pets or guards to agglutinate on keep doors.
            // Ideally, we should check for an alternative path.
            foreach (List<GameDoorBase> doorsAroundNode in _doorsOnPath.Values)
            {
                foreach (GameDoorBase door in doorsAroundNode)
                {
                    if (door.State is eDoorState.Closed && !door.CanBeOpenedViaInteraction)
                    {
                        nextNode = null;
                        return false;
                    }
                }
            }

            // Dequeue the next node if we're close to it, and any subsequent node that might be close.
            // Prevent corner-cutting by raycasting to the next node before removing the current one.
            // Open any doors associated with the node as we reach it.
            if (_pathNodes.TryPeek(0, out WrappedPathfindingNode current) && Owner.IsWithinRadius(current.Position, NODE_REACHED_DISTANCE))
            {
                int nodesToRemove = 0;
                int count = _pathNodes.Count;

                for (int i = 1; i < count; i++)
                {
                    Vector3 candidatePosition = _pathNodes.Peek(i).Position;

                    if (!Owner.IsWithinRadius(candidatePosition, NODE_MAX_SKIP_DISTANCE))
                        break;

                    if (!PathfindingProvider.Instance.HasLineOfSight(zone, position, candidatePosition))
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
                    WrappedPathfindingNode node = _pathNodes.Dequeue();

                    if (_doorsOnPath.Remove(node, out List<GameDoorBase> doors))
                    {
                        foreach (GameDoorBase door in doors)
                        {
                            if (door.CanBeOpenedViaInteraction && door.State is not eDoorState.Open)
                                door.Open();
                        }
                    }
                }
            }

            if (_pathNodes.Count == 0)
            {
                nextNode = null;
                return false;
            }

            nextNode = _pathNodes.Peek(0).Position;
            return true;
        }

        public bool TryGetClosestReachableNode(Zone zone, Vector3 position, Vector3 target, out Vector3? node)
        {
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
                ReplotPath(zone, position, target);

            if (_pathNodes.Count <= 0)
            {
                node = null;
                return false;
            }

            node = _pathNodes.Peek(_pathNodes.Count - 1).Position;
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
            return $"{nameof(Pathfinder)}[Target={_lastTarget}, Nodes={_pathNodes.Count}, NextNode={(_pathNodes.Count > 0 ? _pathNodes.Peek(0).ToString() : null)}]";
        }
    }
}
