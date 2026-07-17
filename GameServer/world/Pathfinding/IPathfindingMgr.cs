using System;
using System.Numerics;
using OpenDAoC.Pathing;

namespace DOL.GS
{
    public interface IPathfindingMgr
    {
        bool Init();
        void Stop();
        bool RegisterDoor(GameDoorBase door);
        bool UpdateDoorFlags(GameDoorBase door);
        PathfindingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters, Span<WrappedPathfindingNode> nodes);
        Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters);
        Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius, EDtPolyFlags[] filters);
        Vector3? GetClosestPoint(Zone zone, Vector3 position, EDtPolyFlags[] filters);
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

    public readonly record struct PathfindingResult(PathfindingStatus Status, int NodeCount);

    public readonly record struct WrappedPathfindingNode(Vector3 Position, EDtPolyFlags Flags);

    public enum PathfindingStatus
    {
        NotSet,
        NavmeshUnavailable,
        PathFound,
        PartialPathFound,
        BufferTooSmall, // Should still contains a valid path.
        NoPathFound     // Happens when either the start or end point isn't on a mesh polygon.
    }
}
