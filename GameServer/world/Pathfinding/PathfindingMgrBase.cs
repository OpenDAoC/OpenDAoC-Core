using System;
using System.Numerics;

namespace DOL.GS
{
    public abstract class PathfindingMgrBase : IPathfindingMgr
    {
        protected const float CONVERSION_FACTOR = 1.0f / 32f;
        protected const float INV_FACTOR = 1f / CONVERSION_FACTOR;

        public virtual bool Init()
        {
            return true;
        }

        public virtual void Stop() { }

        public virtual bool RegisterDoor(GameDoorBase door)
        {
            return true;
        }

        public virtual bool UpdateDoorFlags(GameDoorBase door)
        {
            return true;
        }

        public virtual PathfindingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters, Span<WrappedPathfindingNode> destination)
        {
            return new(PathfindingStatus.NavmeshUnavailable, 0);
        }

        public virtual Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters)
        {
            return end;
        }

        public virtual Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius, EDtPolyFlags[] filters)
        {
            return position;
        }

        public virtual Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange, EDtPolyFlags[] filters)
        {
            return position;
        }

        public virtual Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset, EDtPolyFlags[] filters)
        {
            return origin;
        }

        public virtual Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight, EDtPolyFlags[] filters)
        {
            return position;
        }

        public virtual Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth, EDtPolyFlags[] filters)
        {
            return position;
        }

        public virtual bool HasLineOfSight(Zone zone, Vector3 position, Vector3 target, EDtPolyFlags[] filters)
        {
            return true;
        }

        public virtual Vector3? GetNearestPoly(Zone zone, Vector3 point, EDtPolyFlags[] filters)
        {
            return point;
        }

        public virtual bool HasNavmesh(Zone zone)
        {
            return false;
        }

        public virtual bool IsAvailable => false;
        public virtual EDtPolyFlags[] DefaultFilters => [EDtPolyFlags.All ^ EDtPolyFlags.Disabled, 0];
        public virtual EDtPolyFlags[] BlockingDoorAvoidanceFilters => [DefaultFilters[0], EDtPolyFlags.BlockingDoor];

        protected static void FillRecastFloats(Vector3 value, Span<float> destination)
        {
            destination[0] = value.X * CONVERSION_FACTOR;
            destination[1] = value.Z * CONVERSION_FACTOR;
            destination[2] = value.Y * CONVERSION_FACTOR;
        }

        protected static float[] GetRecastFloats(Vector3 source)
        {
            return
            [
                source[0] * CONVERSION_FACTOR,
                source[2] * CONVERSION_FACTOR,
                source[1] * CONVERSION_FACTOR,
            ];
        }
    }
}
