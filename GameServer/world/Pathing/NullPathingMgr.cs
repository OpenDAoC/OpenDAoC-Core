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

        public Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange = 256, float yRange = 256, float zRange = 256)
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
