using System;
using System.Numerics;

namespace DOL.GS
{
    public class NullPathfindingMgr : IPathfindingMgr
    {
        public bool Init()
        {
            return true;
        }

        public void Stop() { }

        public PathfindingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, Span<WrappedPathfindingNode> destination)
        {
            return new(PathfindingStatus.NavmeshUnavailable, 0);
        }

        public Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end)
        {
            return end;
        }

        public Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius)
        {
            return position;
        }

        public Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange)
        {
            return position;
        }

        public Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset)
        {
            return origin;
        }

        public Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight)
        {
            return position;
        }

        public Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth)
        {
            return position;
        }

        public bool HasLineOfSight(Zone zone, Vector3 position, Vector3 target)
        {
            return true;
        }

        public Vector3? GetNearestPoly(Zone zone, Vector3 point)
        {
            return point;
        }

        public bool HasNavmesh(Zone zone)
        {
            return false;
        }

        public bool IsAvailable => false;
    }
}
