using System;
using System.Numerics;

namespace DOL.GS
{
    public readonly struct PathingResult
    {
        public EPathingError Error { get; }
        public int PointCount { get; }

        public PathingResult(EPathingError error, int pointCount)
        {
            Error = error;
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

    public enum EPathingError
    {
        Unknown,
        PathFound,
        PartialPathFound,
        NoPathFound,
        NavmeshUnavailable,
        BufferTooSmall
    }
}
