using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DOL.GS
{
    public partial class LocalPathingMgr : IPathingMgr
    {
        [Flags]
        private enum EDtStatus : uint
        {
            // High level status.
            DT_SUCCESS = 1u << 30,      // Operation succeed.

            // Detail information for status.
            DT_PARTIAL_RESULT = 1 << 6, // Query did not reach the end location, returning best guess.
        }

        public enum EDtStraightPathOptions : uint
        {
            DT_STRAIGHTPATH_NO_CROSSINGS = 0x00,   // Do not add extra vertices on polygon edge crossings.
            DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01, // Add a vertex at every polygon edge crossing where area changes.
            DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02,  // Add a vertex at every polygon edge crossing.
        }

        private class NavMeshQuery : IDisposable
        {
            IntPtr _query;

            public NavMeshQuery(IntPtr navMesh)
            {
                if (!CreateNavMeshQuery(navMesh, ref this._query))
                    throw new Exception("can't create NavMeshQuery");
            }

            public void Dispose()
            {
                if (_query != IntPtr.Zero)
                    FreeNavMeshQuery(_query);
            }

            public static implicit operator IntPtr(NavMeshQuery query)
            {
                return query._query;
            }
        }

        public const float CONVERSION_FACTOR = 1.0f / 32f;
        private const int MAX_POLY = 256; // Max vector3 when looking up a path (for straight paths too).
        private const float INV_FACTOR = 1f / CONVERSION_FACTOR;

        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static Dictionary<ushort, IntPtr> _navmeshPtrs = [];
        private static readonly Lock _navmeshPtrsLock = new();
        private static ThreadLocal<Dictionary<ushort, NavMeshQuery>> _navmeshQueries = new(() => []);

        [LibraryImport("lib/Detour", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LoadNavMesh(string file, ref IntPtr meshPtr);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool FreeNavMesh(IntPtr meshPtr);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateNavMeshQuery(IntPtr meshPtr, ref IntPtr queryPtr);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool FreeNavMeshQuery(IntPtr queryPtr);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus PathStraight(
            IntPtr queryPtr,
            ReadOnlySpan<float> start,
            ReadOnlySpan<float> end,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            EDtStraightPathOptions pathOptions,
            out int pointCount,
            Span<float> pointBuffer,
            Span<EDtPolyFlags> pointFlags);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus FindRandomPointAroundCircle(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            float radius,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            Span<float> outputVector);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus FindClosestPoint(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            Span<float> outputVector);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus GetPolyAt(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            out uint outputPolyRef,
            Span<float> outputVector);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus SetPolyFlags(IntPtr meshPtr, uint polyRef, EDtPolyFlags flags);

        [LibraryImport("lib/Detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus QueryPolygons(IntPtr queryPtr,
            ReadOnlySpan<float> center,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            Span<uint> outputPolyRefs,
            out int outputPolyCount,
            int maxPolyCount);

        public bool Init()
        {
            try
            {
                nint dummy = IntPtr.Zero;
                LoadNavMesh("this file does not exists!", ref dummy);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("PathingMgr did not find the Detour library", e);

                return false;
            }

            Parallel.ForEach(WorldMgr.Zones.Values, LoadNavMesh);
            return true;
        }

        public static void LoadNavMesh(Zone zone)
        {
            ushort id = zone.ID;
            string path = Path.GetFullPath(Path.Join("pathing", $"zone{id:D3}.nav"));

            if (!File.Exists(path))
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Loading NavMesh failed for zone {id}! (File not found: {path})");

                return;
            }

            nint meshPtr = IntPtr.Zero;

            if (!LoadNavMesh(path, ref meshPtr))
            {
                if (log.IsErrorEnabled)
                    log.Error($"Loading NavMesh failed for zone {id}!");

                return;
            }

            if (meshPtr == IntPtr.Zero)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Loading NavMesh failed for zone {id}! (Pointer was zero!)");

                return;
            }

            if (log.IsInfoEnabled)
                log.Info($"Loading NavMesh successful for zone {id}");

            lock (_navmeshPtrsLock)
            {
                _navmeshPtrs[zone.ID] = meshPtr;
            }

            zone.IsPathingEnabled = true;
        }

        public static void UnloadNavMesh(Zone zone)
        {
            if (!_navmeshPtrs.TryGetValue(zone.ID, out nint ptr))
                return;

            zone.IsPathingEnabled = false;
            FreeNavMesh(ptr);
            _navmeshPtrs.Remove(zone.ID);
        }

        public void Stop()
        {
            foreach (nint ptr in _navmeshPtrs.Values)
                FreeNavMesh(ptr);

            _navmeshPtrs.Clear();
        }

        private static void FillRecastFloats(Vector3 value, Span<float> destination)
        {
            destination[0] = value.X * CONVERSION_FACTOR;
            destination[1] = value.Z * CONVERSION_FACTOR;
            destination[2] = value.Y * CONVERSION_FACTOR;
        }

        private static bool TryGetQuery(Zone zone, out NavMeshQuery query)
        {
            if (!_navmeshPtrs.TryGetValue(zone.ID, out nint ptr))
            {
                query = null;
                return false;
            }

            if (!_navmeshQueries.Value.TryGetValue(zone.ID, out query))
            {
                query = new(ptr);
                _navmeshQueries.Value.Add(zone.ID, query);
            }

            return true;
        }

        public WrappedPathingResult GetPathStraightAsync(Zone zone, Vector3 start, Vector3 end)
        {
            if (!TryGetQuery(zone, out NavMeshQuery query))
                return new(EPathingError.NoPathFound, []);

            Span<float> startFloats = stackalloc float[3];
            FillRecastFloats(start + Vector3.UnitZ * 8, startFloats);

            Span<float> endFloats = stackalloc float[3];
            FillRecastFloats(end + Vector3.UnitZ * 8, endFloats);

            Span<float> polyExt = stackalloc float[3];
            FillRecastFloats(new(64, 64, 256), polyExt);

            EDtPolyFlags includeFilter = EDtPolyFlags.ALL ^ EDtPolyFlags.DISABLED;
            ReadOnlySpan<EDtPolyFlags> filter = [includeFilter, 0];
            EDtStraightPathOptions options = EDtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS;

            float[] rentedBuffer = ArrayPool<float>.Shared.Rent(MAX_POLY * 3);
            EDtPolyFlags[] rentedFlags = ArrayPool<EDtPolyFlags>.Shared.Rent(MAX_POLY);

            try
            {
                Span<float> buffer = rentedBuffer.AsSpan(0, MAX_POLY * 3);
                Span<EDtPolyFlags> flags = rentedFlags.AsSpan(0, MAX_POLY);

                EDtStatus status = PathStraight(query, startFloats, endFloats, polyExt, filter, options, out int numNodes, buffer, flags);

                if ((status & EDtStatus.DT_SUCCESS) == 0)
                    return new(EPathingError.NoPathFound, []);

                WrappedPathPoint[] points = new WrappedPathPoint[numNodes];

                for (int i = 0; i < numNodes; i++)
                    points[i] = new(new(buffer[i * 3 + 0] * INV_FACTOR, buffer[i * 3 + 2] * INV_FACTOR, buffer[i * 3 + 1] * INV_FACTOR), flags[i]);

                return new(EPathingError.PartialPathFound, points);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(rentedBuffer);
                ArrayPool<EDtPolyFlags>.Shared.Return(rentedFlags);
            }
        }

        public Vector3? GetRandomPointAsync(Zone zone, Vector3 position, float radius)
        {
            if (!TryGetQuery(zone, out NavMeshQuery query))
                return null;

            Span<float> center = stackalloc float[3];
            FillRecastFloats(position + Vector3.UnitZ * 8, center);

            EDtPolyFlags defaultInclude = EDtPolyFlags.ALL ^ EDtPolyFlags.DISABLED;
            ReadOnlySpan<EDtPolyFlags> filter = [defaultInclude, 0];
            ReadOnlySpan<float> polyPickEx = [2.0f, 4.0f, 2.0f];

            Span<float> outVec = stackalloc float[3];
            EDtStatus status = FindRandomPointAroundCircle(query, center, radius * CONVERSION_FACTOR, polyPickEx, filter, outVec);

            return (status & EDtStatus.DT_SUCCESS) == 0 ? null : new(outVec[0] * INV_FACTOR, outVec[2] * INV_FACTOR, outVec[1] * INV_FACTOR);
        }

        public Vector3? GetClosestPointAsync(Zone zone, Vector3 position, float xRange = 256f, float yRange = 256f, float zRange = 256f)
        {
            // Assume the point is safe if we don't have a navmesh.
            if (!TryGetQuery(zone, out NavMeshQuery query))
                return position;

            Span<float> center = stackalloc float[3];
            FillRecastFloats(position + Vector3.UnitZ * 8, center);

            EDtPolyFlags defaultInclude = EDtPolyFlags.ALL ^ EDtPolyFlags.DISABLED;
            ReadOnlySpan<EDtPolyFlags> filter = [defaultInclude, 0];

            Span<float> polyPickEx = stackalloc float[3];
            FillRecastFloats(new Vector3(xRange, yRange, zRange), polyPickEx);

            Span<float> outVec = stackalloc float[3];
            EDtStatus status = FindClosestPoint(query, center, polyPickEx, filter, outVec);

            return (status & EDtStatus.DT_SUCCESS) == 0 ? null : new(outVec[0] * INV_FACTOR, outVec[2] * INV_FACTOR, outVec[1] * INV_FACTOR);
        }

        public bool HasNavmesh(Zone zone)
        {
            return zone != null && _navmeshPtrs.ContainsKey(zone.ID);
        }

        public bool IsAvailable => true;
    }
}
