using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using DOL.GS.Movement;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class PathCalculator
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int MIN_TARGET_DIFF_REPLOT_DISTANCE = 64;
        public const int NODE_REACHED_DISTANCE = 16;
        public const int NODE_REACHED_DISTANCE_STRICT = 2;
        public const int DOOR_SEARCH_DISTANCE = 64;

        public GameNPC Owner { get; }
        public bool ForceReplot { get; set; }
        public PathingStatus PathingStatus { get; private set; }

        private RingQueue<WrappedPathPoint> _pathNodes = new();
        private Dictionary<WrappedPathPoint, List<GameDoorBase>> _doorsOnPath = new();
        private Vector3 _lastTarget = Vector3.Zero;
        private PathVisualization _pathVisualization;

        public PathCalculator(GameNPC owner)
        {
            ForceReplot = true;
            Owner = owner;
        }

        public void Clear()
        {
            _pathNodes.Clear();
            _doorsOnPath.Clear();
            _lastTarget = Vector3.Zero;
            PathingStatus = PathingStatus.NotSet;
            ForceReplot = true;
        }

        public bool ShouldPath(Zone zone,Vector3 target)
        {
            if (Owner.Flags.HasFlag(GameNPC.eFlags.FLYING) || Owner is GameTaxi || Owner is GameTaxiBoat)
                return false;

            if (zone == null || !zone.IsPathingEnabled)
                return false;

            // Target is in a different zone (TODO: implement this maybe? not sure if really required).
            if (Owner.CurrentRegion.GetZone((int) target.X, (int) target.Y) != zone)
                return false;

            return true;
        }

        private void ReplotPath(Zone zone, Vector3 position, Vector3 target)
        {
            const int MAX_PATH_NODES = 512;
            WrappedPathPoint[] rentedPathBuffer = ArrayPool<WrappedPathPoint>.Shared.Rent(MAX_PATH_NODES);

            try
            {
                PathingResult pathingResult = PathingMgr.Instance.GetPathStraight(zone, position, target, rentedPathBuffer);

                _pathNodes.Clear();
                _doorsOnPath.Clear();

                if (pathingResult.Status is PathingStatus.BufferTooSmall)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Path buffer for {Owner} was too small. Needed {pathingResult.PointCount}, had {MAX_PATH_NODES}.");
                }

                int nodeCount = Math.Min(pathingResult.PointCount, MAX_PATH_NODES);

                if (nodeCount > 0)
                {
                    // Keep track of doors on the path.
                    // Skip the first node if it isn't a door.
                    for (int i = 0; i < nodeCount; i++)
                    {
                        WrappedPathPoint pathPoint = rentedPathBuffer[i];
                        bool isDoor = (pathPoint.Flags & EDtPolyFlags.DOOR) != 0;

                        if (i == 0 && !isDoor)
                            continue;

                        _pathNodes.Enqueue(pathPoint);

                        if (!isDoor)
                            continue;

                        Point3D point = new(pathPoint.Position.X, pathPoint.Position.Y, pathPoint.Position.Z);
                        _doorsOnPath[pathPoint] = new();
                        Owner.CurrentRegion.GetInRadius(point, eGameObjectType.DOOR, DOOR_SEARCH_DISTANCE, _doorsOnPath[pathPoint]);
                    }
                }

                _pathVisualization?.Visualize(_pathNodes, Owner.CurrentRegion);
                _lastTarget = target;
                PathingStatus = pathingResult.Status;
                ForceReplot = false;
            }
            finally
            {
                ArrayPool<WrappedPathPoint>.Shared.Return(rentedPathBuffer);
            }
        }

        /// <summary>
        /// Calculates the next point this NPC should walk to to reach the target
        /// </summary>
        public bool TryGetNextNode(Zone zone, Vector3 position, Vector3 target, out Vector3? nextNode)
        {
            // Check if we can reuse our path. We assume that we ourselves never "suddenly" warp to a completely different pos.
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
                ReplotPath(zone, position, target);

            // Stop right there if the path contains a closed door that we're not allowed to open.
            // We don't want pets or guards to agglutinate on keep doors.
            // Ideally, we should check for an alternative path.
            foreach (List<GameDoorBase> doorsAroundPathPoint in _doorsOnPath.Values)
            {
                foreach (GameDoorBase door in doorsAroundPathPoint)
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
            if (_pathNodes.TryPeek(0, out WrappedPathPoint current) && Owner.IsWithinRadius(current.Position, NODE_REACHED_DISTANCE))
            {
                int nodesToRemove = 0;
                int count = _pathNodes.Count;

                for (int i = 1; i < count; i++)
                {
                    WrappedPathPoint candidate = _pathNodes.Peek(i);

                    if (!PathingMgr.Instance.HasLineOfSight(zone, position, candidate.Position))
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
                    WrappedPathPoint node = _pathNodes.Dequeue();

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
            return $"PathCalc[Target={_lastTarget}, Nodes={_pathNodes.Count}, NextNode={(_pathNodes.Count > 0 ? _pathNodes.Peek(0).ToString() : null)}]";
        }
    }
}
