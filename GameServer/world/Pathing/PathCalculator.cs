using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Threading;

namespace DOL.GS
{
    public sealed class PathCalculator
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int MIN_PATHING_DISTANCE = 80;
        public const int MIN_TARGET_DIFF_REPLOT_DISTANCE = 80;
        public const int NODE_REACHED_DISTANCE = 24;
        private const int IDLE = 0;
        private const int REPLOTTING = 1;

        public GameNPC Owner { get; }
        public bool ForceReplot { get; set; }
        public bool DidFindPath { get; private set; }

        private Queue<WrappedPathPoint> _pathNodes = new();
        private Vector3 _lastTarget = Vector3.Zero;
        private int _isReplottingPath = IDLE;

        public static bool IsSupported(GameNPC npc)
        {
            return npc?.CurrentZone != null && npc.CurrentZone.IsPathingEnabled;
        }

        public PathCalculator(GameNPC owner)
        {
            ForceReplot = true;
            Owner = owner;
        }

        private bool ShouldPath(Vector3 target)
        {
            return ShouldPath(Owner, target);
        }

        public void Clear()
        {
            _pathNodes.Clear();
            _lastTarget = Vector3.Zero;
            DidFindPath = false;
            ForceReplot = true;
        }

        public static bool ShouldPath(GameNPC owner, Vector3 target)
        {
            // Too close to path (note: this is making NPCs walk through walls).
            if (owner.GetDistanceTo(target) < MIN_PATHING_DISTANCE)
                return false;

            if (owner.Flags.HasFlag(GameNPC.eFlags.FLYING))
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
                WrappedPathingResult pathingResult = PathingMgr.Instance.GetPathStraightAsync(currentZone, currentPos, target);
                _pathNodes.Clear();

                if (pathingResult.Error is not EPathingError.NoPathFound && pathingResult.Error is not EPathingError.NavmeshUnavailable && pathingResult.Points != null)
                {
                    DidFindPath = true;
                    int to = pathingResult.Points.Length - 1; // Remove target node only if no partial path.

                    if (pathingResult.Error is EPathingError.PartialPathFound)
                        to = pathingResult.Points.Length;

                    for (int i = 0; i < to; i++)
                    {
                        WrappedPathPoint pt = pathingResult.Points[i];

                        // Skip the first node.
                        if (i == 0)
                            continue;

                        _pathNodes.Enqueue(pt);
                    }
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
                noPathReason = ENoPathReason.NoProblem;
                return null;
            }

            // Check if we can reuse our path. We assume that we ourselves never "suddenly" warp to a completely different position.
            if (ForceReplot || !_lastTarget.IsInRange(target, MIN_TARGET_DIFF_REPLOT_DISTANCE))
                ReplotPath(target);

            // Find the next node in the path to the target, but skip points that are too close.
            while (_pathNodes.Count > 0 && Owner.IsWithinRadius(_pathNodes.Peek().Position, NODE_REACHED_DISTANCE))
                _pathNodes.Dequeue();

            if (_pathNodes.Count == 0)
            {
                if (!DidFindPath)
                {
                    noPathReason = ENoPathReason.NoPath;
                    return null;
                }

                noPathReason = ENoPathReason.End;
                return null;
            }

            noPathReason = ENoPathReason.NoProblem;
            return _pathNodes.Peek().Position;
        }

        public override string ToString()
        {
            return $"PathCalc[Target={_lastTarget}, Nodes={_pathNodes.Count}, NextNode={(_pathNodes.Count > 0 ? _pathNodes.Peek().ToString() : null)}]";
        }
    }
}
