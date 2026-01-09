using System;
using System.Numerics;

namespace DOL.GS
{
    public interface IPathingMgr
    {
        /// <summary>
        ///   Initializes the PathingMgr  by loading all available navmeshes
        /// </summary>
        bool Init();

        /// <summary>
        ///   Stops the PathingMgr and releases all loaded navmeshes
        /// </summary>
        void Stop();

        /// <summary>
        ///   Returns a path that prevents collisions with the navmesh, but floats freely otherwise
        /// </summary>
        PathingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, Span<WrappedPathPoint> destination);

        /// <summary>
        ///   Returns a random point on the navmesh around the given position
        /// </summary>
        Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius);

        /// <summary>
        ///   Returns the closest point on the navmesh, if available, or no point found.
        ///   Returns the input position if no navmesh is available
        /// </summary>
        Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange);
        Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset, Vector3? referencePos);
        Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight);
        Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth);

        /// <summary>
        ///   True if pathing is enabled for the specified zone
        /// </summary>
        bool HasNavmesh(Zone zone);

        /// <summary>
        /// True if currently running & working
        /// </summary>
        bool IsAvailable { get; }
    }
}
