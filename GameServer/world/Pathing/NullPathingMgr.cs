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

        public WrappedPathingResult GetPathStraightAsync(Zone zone, Vector3 start, Vector3 end)
        {
            return new WrappedPathingResult(EPathingError.NavmeshUnavailable, []);
        }

        public Vector3? GetRandomPointAsync(Zone zone, Vector3 position, float radius)
        {
            return null;
        }

        public Vector3? GetClosestPointAsync(Zone zone, Vector3 position, float xRange = 256, float yRange = 256, float zRange = 256)
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
