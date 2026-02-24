using System;
using System.Numerics;

namespace DOL.GS
{
    public interface IPathfindingMgr
    {
        bool Init();
        void Stop();
        bool RegisterDoor(GameDoorBase door);
        bool UpdateDoorFlags(GameDoorBase door);
        PathfindingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters, Span<WrappedPathfindingNode> destination);
        Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters);
        Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius, EDtPolyFlags[] filters);
        Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange, EDtPolyFlags[] filters);
        Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset, EDtPolyFlags[] filters);
        Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight, EDtPolyFlags[] filters);
        Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth, EDtPolyFlags[] filters);
        bool TrySnapToMesh(Zone zone, ref Vector3 position, float range);
        bool HasLineOfSight(Zone zone, Vector3 position, Vector3 target, EDtPolyFlags[] filters);
        bool HasNavmesh(Zone zone);
        bool IsAvailable { get; }
        EDtPolyFlags[] DefaultFilters { get; }
        EDtPolyFlags[] BlockingDoorAvoidanceFilters { get; }
    }

    public readonly struct PathfindingResult
    {
        public PathfindingStatus Status { get; }
        public int NodeCount { get; }

        public PathfindingResult(PathfindingStatus status, int nodeCount)
        {
            Status = status;
            NodeCount = nodeCount;
        }
    }

    public readonly struct WrappedPathfindingNode : IEquatable<WrappedPathfindingNode>
    {
        public Vector3 Position { get; }
        public EDtPolyFlags Flags { get; }

        public WrappedPathfindingNode(Vector3 position, EDtPolyFlags flags)
        {
            Position = position;
            Flags = flags;
        }

        public override string ToString()
        {
            return $"({Position}, {Flags})";
        }

        public bool Equals(WrappedPathfindingNode other)
        {
            return Position.Equals(other.Position) && Flags == other.Flags;
        }

        public override bool Equals(object obj)
        {
            return obj is WrappedPathfindingNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Flags);
        }

        public static bool operator ==(WrappedPathfindingNode left, WrappedPathfindingNode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WrappedPathfindingNode left, WrappedPathfindingNode right)
        {
            return !(left == right);
        }
    }

    public enum PathfindingStatus
    {
        NotSet,
        NavmeshUnavailable,
        PathFound,
        PartialPathFound,
        BufferTooSmall, // Should still contains a valid path.
        NoPathFound     // Happens when either the start or end point isn't on a mesh polygon.
    }

    [Flags]
    public enum EDtPolyFlags : ushort
    {
        Walk = 0x01,         // Ability to walk (ground, grass, road)
        Swim = 0x02,         // Ability to swim (water).
        Door = 0x04,         // Ability to move through doors.
        Jump = 0x08,         // Ability to jump.
        Disabled = 0x10,     // Disabled polygon
        All = 0xffff,        // All abilities.

        // Run-time flags (not stored in the navmesh, only used for custom pathfinding logic)
        BlockingDoor = 0x20, // Door that blocks movement when closed.
        AnyDoor = Door | BlockingDoor
    }

    [Flags]
    public enum EDtStatus : uint
    {
        // High level status.
        DT_FAILURE = 1u << 31,          // Operation failed.
        DT_SUCCESS = 1u << 30,          // Operation succeed.
        DT_IN_PROGRESS = 1u << 29,      // Operation still in progress.

        // Detail information for status.
        DT_STATUS_DETAIL_MASK = 0x0FFFFFFF,
        DT_WRONG_MAGIC = 1 << 0,        // Input data is not recognized.
        DT_WRONG_VERSION = 1 << 1,      // Input data is in wrong version.
        DT_OUT_OF_MEMORY = 1 << 2,      // Operation ran out of memory.
        DT_INVALID_PARAM = 1 << 3,      // An input parameter was invalid.
        DT_BUFFER_TOO_SMALL = 1 << 4,   // Result buffer for the query was too small to store all results.
        DT_OUT_OF_NODES = 1 << 5,       // Query ran out of nodes during search.
        DT_PARTIAL_RESULT = 1 << 6,     // Query did not reach the end location, returning best guess.
        DT_ALREADY_OCCUPIED = 1 << 7    // A tile has already been assigned to the given x,y coordinate.
    }

    public enum EDtStraightPathOptions : uint
    {
        DT_STRAIGHTPATH_NO_CROSSINGS = 0x00,   // Do not add extra vertices on polygon edge crossings.
        DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01, // Add a vertex at every polygon edge crossing where area changes.
        DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02,  // Add a vertex at every polygon edge crossing.
    }
}
