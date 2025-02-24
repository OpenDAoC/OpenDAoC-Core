using System;
using System.Numerics;

namespace DOL.GS
{
    public class WrappedPathingResult
    {
        public EPathingError Error;
        public WrappedPathPoint[] Points;

        public WrappedPathingResult() { }

        public WrappedPathingResult(EPathingError error, WrappedPathPoint[] points)
        {
            Error = error;
            Points = points;
        }
    }

    public class WrappedPathPoint
    {
        public Vector3 Position;
        public EDtPolyFlags Flags;

        public WrappedPathPoint() { }

        public WrappedPathPoint(Vector3 position, EDtPolyFlags flags)
        {
            Position = position;
            Flags = flags;
        }

        public override string ToString()
        {
            return $"({Position}, {Flags})";
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
        UNKNOWN = 0,
        PathFound = 1,
        PartialPathFound = 2,
        NoPathFound = 3,
        NavmeshUnavailable = 4
    }
}
