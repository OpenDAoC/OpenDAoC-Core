using System;
using System.Numerics;

namespace DOL.GS
{
    public interface IPathingMgr
    {
        bool Init();
        void Stop();
        PathingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, Span<WrappedPathPoint> destination);
        Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end);
        Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius);
        Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange);
        Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset);
        Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight);
        Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth);
        bool HasLineOfSight(Zone zone, Vector3 position, Vector3 target);
        Vector3? GetNearestPoly(Zone zone, Vector3 point);
        bool HasNavmesh(Zone zone);
        bool IsAvailable { get; }
    }

    public readonly struct PathingResult
    {
        public PathingStatus Status { get; }
        public int PointCount { get; }

        public PathingResult(PathingStatus status, int pointCount)
        {
            Status = status;
            PointCount = pointCount;
        }
    }

    public readonly struct WrappedPathPoint : IEquatable<WrappedPathPoint>
    {
        public Vector3 Position { get; }
        public EDtPolyFlags Flags { get; }

        public WrappedPathPoint(Vector3 position, EDtPolyFlags flags)
        {
            Position = position;
            Flags = flags;
        }

        public override string ToString()
        {
            return $"({Position}, {Flags})";
        }

        public bool Equals(WrappedPathPoint other)
        {
            return Position.Equals(other.Position) && Flags == other.Flags;
        }

        public override bool Equals(object obj)
        {
            return obj is WrappedPathPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Flags);
        }

        public static bool operator ==(WrappedPathPoint left, WrappedPathPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WrappedPathPoint left, WrappedPathPoint right)
        {
            return !(left == right);
        }
    }

    [Flags]
    public enum EDtPolyFlags : ushort
    {
        WALK = 0x01, // Ability to walk (ground, grass, road)
        SWIM = 0x02, // Ability to swim (water).
        DOOR = 0x04, // Ability to move through doors.
        JUMP = 0x08, // Ability to jump.
        DISABLED = 0x10, // Disabled polygon
        DOOR_ALB = 0x20,
        DOOR_MID = 0x40,
        DOOR_HIB = 0x80,
        ALL = 0xffff // All abilities.
    }

    public enum PathingStatus
    {
        NotSet,
        NavmeshUnavailable,
        PathFound,
        PartialPathFound,
        BufferTooSmall, // Should still contains a valid path.
        NoPathFound // Happens when either the start or end point isn't on a mesh polygon.
    }
}
