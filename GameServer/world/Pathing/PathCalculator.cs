using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Threading;
using DOL.GS.Movement;

namespace DOL.GS
{
    public sealed class PathCalculator
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int MIN_PATHING_DISTANCE = 80;
        public const int MIN_TARGET_DIFF_REPLOT_DISTANCE = 80;
        public const int NODE_REACHED_DISTANCE = 24;
        public const int DOOR_SEARCH_DISTANCE = 512;
        private const int IDLE = 0;
        private const int REPLOTTING = 1;

        public GameNPC Owner { get; }
        public bool ForceReplot { get; set; }
        public bool DidFindPath { get; private set; }

        private Queue<WrappedPathPoint> _pathNodes = new();
        private Dictionary<WrappedPathPoint, List<GameDoorBase>> _doorsOnPath = new();
        private Vector3 _lastTarget = Vector3.Zero;
        private int _isReplottingPath = IDLE;
        private PathVisualization _pathVisualization;

        public PathCalculator(GameNPC owner)
        {
            ForceReplot = true;
            Owner = owner;
        }

        public static bool IsSupported(GameNPC npc)
        {
            return npc?.CurrentZone != null && npc.CurrentZone.IsPathingEnabled;
        }

        private bool ShouldPath(Vector3 target)
        {
            return ShouldPath(Owner, target);
        }

        public void Clear()
        {
            _pathNodes.Clear();
            _doorsOnPath.Clear();
            _lastTarget = Vector3.Zero;
            DidFindPath = false;
            ForceReplot = true;
        }

        public static bool ShouldPath(GameNPC owner, Vector3 target)
        {
            // Too close to path (note: this is making NPCs walk through walls).
            if (owner.GetDistanceTo(target) < MIN_PATHING_DISTANCE)
                return false;

            if (owner.Flags.HasFlag(GameNPC.eFlags.FLYING) || owner is GameTaxi)
                return false;

            // This will probably result in some really awkward paths otherwise.
            if (owner.Z <= 0)
                return false;

            Zone zone = owner.CurrentZone;

            if (zone == null || !zone.IsPathingEnabled)
                return false;

            // Target is in a different zone (TODO: implement this maybe? not sure if really required).
            if (owner.CurrentRegion.GetZone((int) target.X, (int) target.Y) != zone)
                return false;

            return true;
        }

        private void ReplotPath(Vector3 target)
        {
            // Try acquiring a pathing lock.
            if (Interlocked.CompareExchange(ref _isReplottingPath, REPLOTTING, IDLE) != IDLE)
                return;

            try
            {
                Zone currentZone = Owner.CurrentZone;
                Vector3 currentPos = new(Owner.X, Owner.Y, Owner.Z);
                WrappedPathingResult pathingResult = PathingMgr.Instance.GetPathStraight(currentZone, currentPos, target);
                _pathNodes.Clear();
                _doorsOnPath.Clear();

                if (pathingResult.Error is not EPathingError.NoPathFound && pathingResult.Error is not EPathingError.NavmeshUnavailable && pathingResult.Points != null)
                {
                    DidFindPath = true;
                    int to = pathingResult.Points.Length - 1; // Remove target node only if no partial path.

                    if (pathingResult.Error is EPathingError.PartialPathFound)
                        to = pathingResult.Points.Length;

                    for (int i = 0; i < to; i++)
                    {
                        WrappedPathPoint pathPoint = pathingResult.Points[i];
                        bool isDoor = (pathPoint.Flags & EDtPolyFlags.DOOR) != 0;

                        // Skip the first node if it isn't a door.
                        if (i == 0 && !isDoor)
                            continue;

                        _pathNodes.Enqueue(pathPoint);

                        // Keep track of doors on the path.
                        if (!isDoor)
                            continue;

                        Point3D point = new(pathPoint.Position.X, pathPoint.Position.Y, pathPoint.Position.Z); // Should find a way to avoid this conversion.
                        _doorsOnPath[pathPoint] = new();
                        Owner.CurrentRegion.GetInRadius(point, eGameObjectType.DOOR, DOOR_SEARCH_DISTANCE, _doorsOnPath[pathPoint]);
                    }

                    _pathVisualization?.Visualize(_pathNodes, Owner.CurrentRegion);
                }
                else
                    DidFindPath = false;

                _lastTarget = target;
                ForceReplot = false;
            }
            finally
            {
                if (Interlocked.Exchange(ref _isReplottingPath, IDLE) != REPLOTTING)
                {
                    if (log.IsErrorEnabled)
                        log.Error("PathCalc semaphore was in IDLE state even though we were replotting. This should never happen");
                }
            }
        }

        /// <summary>
        /// Calculates the next point this NPC should walk to to reach the target
        /// </summary>
        /// <returns>Next path node, or null if target reached. Throws a NoPathToTargetException if path is blocked</returns>
        public Vector3? CalculateNextTarget(Vector3 target, out ENoPathReason noPathReason)
        {
            if (!ShouldPath(target))
            {
                DidFindPath = true; // Not needed.
                noPathReason = ENoPathReason.NoPath;
                return null;
            }

            // Check if we can reuse our path. We assume that we ourselves never "suddenly" warp to a completely different position.
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
                ReplotPath(target);

            // Stop right there if the path contains a closed door that we're not allowed to open.
            // Ideally, we should check for an alternative path.
            foreach (List<GameDoorBase> doorsAroundPathPoint in _doorsOnPath.Values)
            {
                foreach (GameDoorBase door in doorsAroundPathPoint)
                {
                    if (door.State is eDoorState.Closed && !door.CanBeOpenedViaInteraction)
                    {
                        noPathReason = ENoPathReason.ClosedDoor;
                        return null;
                    }
                }
            }

            // Dequeue the next node if we've reached it, and any subsequent node that might be close to it.
            while (_pathNodes.TryPeek(out WrappedPathPoint nextNode) && Owner.IsWithinRadius(nextNode.Position, NODE_REACHED_DISTANCE))
            {
                // Open doors that can be opened (which should be every door if we're here).
                if (_doorsOnPath.Remove(nextNode, out List<GameDoorBase> doorsAroundPathPoint))
                {
                    foreach (GameDoorBase door in doorsAroundPathPoint)
                    {
                        if (door.CanBeOpenedViaInteraction && door.State is not eDoorState.Open)
                            door.Open();
                    }
                }

                _pathNodes.Dequeue();
            }

            if (_pathNodes.Count == 0)
            {
                if (DidFindPath)
                {
                    noPathReason = ENoPathReason.End;
                    return null;
                }

                noPathReason = ENoPathReason.NoPath;
                return null;
            }

            noPathReason = ENoPathReason.NoProblem;
            return _pathNodes.Peek().Position;
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
            return $"PathCalc[Target={_lastTarget}, Nodes={_pathNodes.Count}, NextNode={(_pathNodes.Count > 0 ? _pathNodes.Peek().ToString() : null)}]";
        }
    }
}
