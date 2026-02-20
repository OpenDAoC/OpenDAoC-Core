using System;
using System.Numerics;

namespace DOL.GS
{
    public abstract class PathfindingMgrBase : IPathfindingMgr
    {
        public virtual bool Init()
        {
            return true;
        }

        public virtual void Stop() { }

        public virtual PathfindingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, Span<WrappedPathfindingNode> destination)
        {
            return new(PathfindingStatus.NavmeshUnavailable, 0);
        }

        public virtual Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end)
        {
            return end;
        }

        public virtual Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius)
        {
            return position;
        }

        public virtual Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange)
        {
            return position;
        }

        public virtual Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset)
        {
            return origin;
        }

        public virtual Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight)
        {
            return position;
        }

        public virtual Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth)
        {
            return position;
        }

        public virtual bool HasLineOfSight(Zone zone, Vector3 position, Vector3 target)
        {
            return true;
        }

        public virtual Vector3? GetNearestPoly(Zone zone, Vector3 point)
        {
            return point;
        }

        public virtual bool HasNavmesh(Zone zone)
        {
            return false;
        }

        public virtual bool IsAvailable => false;
    }
}
