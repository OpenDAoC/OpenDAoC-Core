using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DOL.Logging;
using OpenDAoC.Pathing;

namespace DOL.GS
{
    public sealed class LocalPathfindingMgr : PathfindingMgrBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Dictionary<ushort, DetourNavMesh> _navMeshes = new();
        private static readonly Lock _navMeshesLock = new();

        private static readonly Dictionary<GameDoorBase, ulong[]> _doorPolyRefs = new();
        private static readonly Lock _doorPolyRefsLock = new();

        // Fix me:
        // This thread static will contain DetourNavMeshQuery to disposed DetourNavMesh if UnloadZone is called.
        // See ClearQueriesForZone.
        private static readonly ThreadLocal<Dictionary<ushort, DetourNavMeshQuery>> _navmeshQueries = new(() => []);

        private static readonly EDtPolyFlags[] _doorMask = [EDtPolyFlags.Door, 0];
        private static readonly Vector3 _defaultHalfExtents = RecastCoords.DefaultHalfExtents;

        public override bool Init()
        {
            if (!DetourNavMesh.TryProbeNativeLibrary(out Exception error))
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(LocalPathfindingMgr)} did not find the Detour library", error);

                return false;
            }

            Parallel.ForEach(WorldMgr.Zones.Values, LoadNavMesh);
            return true;
        }

        public static void LoadNavMesh(Zone zone)
        {
            ushort id = zone.ID;
            string path = Path.GetFullPath(Path.Join("navmesh", $"zone{id:D3}.nav"));

            if (!File.Exists(path))
            {
                // Fall back to old "pathing" folder for backwards compatibility.
                path = Path.GetFullPath(Path.Join("pathing", $"zone{id:D3}.nav"));

                if (!File.Exists(path))
                {
                    if (log.IsDebugEnabled)
                        log.Debug($"Loading NavMesh failed for zone {id}! (File not found: {path})");

                    return;
                }
            }

            DetourNavMesh mesh = DetourNavMesh.TryLoad(path);

            if (mesh == null)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Loading NavMesh failed for zone {id}!");

                return;
            }

            if (log.IsInfoEnabled)
                log.Info($"Loading NavMesh successful for zone {id}");

            lock (_navMeshesLock)
            {
                if (_navMeshes.TryGetValue(zone.ID, out DetourNavMesh existing))
                    existing.Dispose();

                _navMeshes[zone.ID] = mesh;
            }

            zone.IsPathfindingEnabled = true;
        }

        public static void UnloadNavMesh(Zone zone)
        {
            DetourNavMesh mesh;

            lock (_navMeshesLock)
            {
                if (!_navMeshes.Remove(zone.ID, out mesh))
                    return;
            }

            zone.IsPathfindingEnabled = false;
            ClearQueriesForZone(zone.ID);
            mesh.Dispose();
        }

        public override void Stop()
        {
            lock (_navMeshesLock)
            {
                foreach (DetourNavMesh mesh in _navMeshes.Values)
                    mesh.Dispose();

                _navMeshes.Clear();
            }

            _doorPolyRefs.Clear();
            _navmeshQueries.Value.Clear();
        }

        public override bool RegisterDoor(GameDoorBase door)
        {
            const int MAX_DOOR_POLYS = 24;

            Zone zone = door.CurrentZone;

            if (zone == null)
                return false;

            ulong[] candidatePolyRefs = GetNearestPolys(
                zone,
                new(door.X, door.Y, door.Z),
                _doorMask,
                128f, 128f, 32f,
                MAX_DOOR_POLYS);

            if (candidatePolyRefs.Length == 0)
                return false;

            lock (_doorPolyRefsLock)
            {
                if (_doorPolyRefs.ContainsKey(door))
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Door (ID: {door.DoorId}) at {door.X},{door.Y},{door.Z} in zone {zone.ID} is already registered.");

                    return false;
                }

                if (log.IsWarnEnabled)
                {
                    HashSet<ulong> claimedPolyRefs = new();

                    foreach (var pair in _doorPolyRefs)
                    {
                        if (pair.Key.CurrentZone?.ID == zone.ID)
                        {
                            foreach (ulong poly in pair.Value)
                                claimedPolyRefs.Add(poly);
                        }
                    }

                    int claimedCount = 0;

                    foreach (ulong poly in candidatePolyRefs)
                    {
                        if (claimedPolyRefs.Contains(poly))
                            claimedCount++;
                    }

                    if (claimedCount == candidatePolyRefs.Length)
                    {
                        log.Warn($"Door (ID: {door.DoorId}) at {door.X},{door.Y},{door.Z} in zone {zone.ID} " +
                            "overlaps entirely with another door's polygons.");
                    }
                    else if (claimedCount > 0)
                    {
                        log.Warn($"Door (ID: {door.DoorId}) at {door.X},{door.Y},{door.Z} in zone {zone.ID} " +
                            $"has {claimedCount} overlapping polygons with another door.");
                    }
                }

                _doorPolyRefs.Add(door, candidatePolyRefs);
            }

            UpdateDoorFlags(zone, door);
            return true;
        }

        public override bool UpdateDoorFlags(GameDoorBase door)
        {
            Zone zone = door.CurrentZone;

            return zone != null && UpdateDoorFlags(zone, door);
        }

        private static bool UpdateDoorFlags(Zone zone, GameDoorBase door)
        {
            ulong[] polyRef;

            lock (_doorPolyRefsLock)
            {
                if (!_doorPolyRefs.TryGetValue(door, out polyRef))
                    return false;
            }

            return TryGetMesh(zone, out DetourNavMesh mesh) && UpdateDoorFlags(mesh, polyRef, door);
        }

        private static bool UpdateDoorFlags(DetourNavMesh mesh, ulong[] polyRefs, GameDoorBase door)
        {
            bool isBlockingDoor = door.State is eDoorState.Closed && !door.CanBeOpenedViaInteraction;
            EDtPolyFlags flagsToRemove = isBlockingDoor ? EDtPolyFlags.Door : EDtPolyFlags.BlockingDoor;
            EDtPolyFlags flagsToAdd = isBlockingDoor ? EDtPolyFlags.BlockingDoor : EDtPolyFlags.Door;
            EDtStatus status = mesh.UpdateFlags(polyRefs, flagsToRemove, flagsToAdd);

            return status.Succeeded();
        }

        private static bool TryGetMesh(Zone zone, out DetourNavMesh mesh)
        {
            lock (_navMeshesLock)
            {
                return _navMeshes.TryGetValue(zone.ID, out mesh);
            }
        }

        private static bool TryGetQuery(Zone zone, out DetourNavMeshQuery query)
        {
            if (!TryGetMesh(zone, out DetourNavMesh mesh))
            {
                query = null;
                return false;
            }

            if (!_navmeshQueries.Value.TryGetValue(zone.ID, out query))
            {
                query = mesh.CreateQuery();
                _navmeshQueries.Value.Add(zone.ID, query);
            }

            return true;
        }

        private static void ClearQueriesForZone(ushort zoneId)
        {
            // ThreadLocal: best-effort clear on the current thread only.
            if (_navmeshQueries.IsValueCreated && _navmeshQueries.Value.Remove(zoneId, out DetourNavMeshQuery query))
                query.Dispose();
        }

        public override PathfindingResult GetPathStraight(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters, Span<WrappedPathfindingNode> nodes)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return new(PathfindingStatus.NavmeshUnavailable, 0);

            Span<Vector3> positions = stackalloc Vector3[DetourNavMeshQuery.MAX_POLY];
            Span<EDtPolyFlags> flags = stackalloc EDtPolyFlags[DetourNavMeshQuery.MAX_POLY];

            PathQueryResult result = query.PathStraight(
                start, end, _defaultHalfExtents, filters,
                EDtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS,
                positions, flags);

            if (!result.Success)
                return new(PathfindingStatus.NoPathFound, 0);

            if (result.BufferTooSmall || nodes.Length < result.PointCount)
                return new(PathfindingStatus.BufferTooSmall, result.PointCount);

            for (int i = 0; i < result.PointCount; i++)
                nodes[i] = new(positions[i], flags[i]);

            PathfindingStatus pathfindingStatus = result.Partial ? PathfindingStatus.PartialPathFound : PathfindingStatus.PathFound;
            return new(pathfindingStatus, result.PointCount);
        }

        public override Vector3? GetMoveAlongSurface(Zone zone, Vector3 start, Vector3 end, EDtPolyFlags[] filters)
        {
            // Confusing name, see Detour docs for what it does.
            // Should not be used for pathfinding, only for small adjustments to positions.

            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return null;

            return query.MoveAlongSurface(start, end, _defaultHalfExtents, filters);
        }

        public override Vector3? GetRandomPoint(Zone zone, Vector3 position, float radius, EDtPolyFlags[] filters)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return null;

            return query.FindRandomPointAroundCircle(position, radius, _defaultHalfExtents, filters);
        }

        public override Vector3? GetClosestPoint(Zone zone, Vector3 position, EDtPolyFlags[] filters)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return null;

            return query.FindClosestPoint(position, _defaultHalfExtents, filters);
        }

        public override Vector3? GetClosestPoint(Zone zone, Vector3 position, float xRange, float yRange, float zRange, EDtPolyFlags[] filters)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return null;

            return query.FindClosestPoint(position, new(xRange, yRange, zRange), filters);
        }

        public override Vector3? GetClosestPointInBounds(Zone zone, Vector3 origin, Vector3 minOffset, Vector3 maxOffset, EDtPolyFlags[] filters)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return null;

            return query.FindClosestPointInBounds(origin, minOffset, maxOffset, filters);
        }

        public override Vector3? GetRoofAbove(Zone zone, Vector3 position, float maxHeight, EDtPolyFlags[] filters)
        {
            const float RADIUS = 3f;

            return GetClosestPointInBounds(
                zone, position,
                new(-RADIUS, -RADIUS, 20f), // Slightly above current position to avoid getting the current floor.
                new(RADIUS, RADIUS, maxHeight),
                filters
            );
        }

        public override Vector3? GetFloorBeneath(Zone zone, Vector3 position, float maxDepth, EDtPolyFlags[] filters)
        {
            const float RADIUS = 3f;

            return GetClosestPointInBounds(
                zone, position,
                new(-RADIUS, -RADIUS, -maxDepth),
                new(RADIUS, RADIUS, 20f), // Slightly above current position to allow getting the current floor.
                filters
            );
        }

        public override bool TrySnapToMesh(Zone zone, ref Vector3 position, float range)
        {
            Vector3? closestPoint = GetClosestPoint(
                zone,
                position,
                range,
                range,
                range,
                PathfindingProvider.Instance.DefaultFilters);

            if (!closestPoint.HasValue)
                return false;

            position = closestPoint.Value;
            return true;
        }

        public override bool HasLineOfSight(Zone zone, Vector3 position, Vector3 target, EDtPolyFlags[] filters)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return false;

            return query.HasLineOfSight(position, target, _defaultHalfExtents, filters);
        }

        private static ulong[] GetNearestPolys(Zone zone, Vector3 point, EDtPolyFlags[] filters, float xRange, float yRange, float zRange, int maxPolyCount)
        {
            if (!TryGetQuery(zone, out DetourNavMeshQuery query))
                return [];

            Span<ulong> polyRefs = stackalloc ulong[maxPolyCount];
            int count = query.GetPolysInBox(point, new(xRange, yRange, zRange), filters, polyRefs);

            if (count == 0)
                return [];

            // Buffer-too-small is not exposed as a separate code path here; the native call may still return success with a truncated set.
            if (count >= maxPolyCount && log.IsWarnEnabled)
                log.Warn($"{nameof(GetNearestPolys)} may have found more than {maxPolyCount} polygons near point {point} in zone {zone.ID}");

            return polyRefs[..count].ToArray();
        }

        public override bool HasNavmesh(Zone zone)
        {
            if (zone == null)
                return false;

            lock (_navMeshesLock)
            {
                return _navMeshes.ContainsKey(zone.ID);
            }
        }

        public override bool IsAvailable => true;
    }
}
