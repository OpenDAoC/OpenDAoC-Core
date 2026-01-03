using System;
using System.Numerics;

namespace DOL.GS
{
    public class NullPathingMgr : IPathingMgr
    {
        public bool Init()
        {
            return true;
        }

        public void Stop() { }

        public PathingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, Span<WrappedPathPoint> destination)
        {
            return new(EPathingError.NavmeshUnavailable, 0);
        }

        public Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius)
        {
            return null;
        }

        public Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange)
        {
            return position;
        }

        public Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset, Vector3? referencePos)
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

        public bool HasNavmesh(Zone zone)
        {
            return false;
        }

        public bool IsAvailable => false;
    }
}
