using System;
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
        public const float CONVERSION_FACTOR = 1.0f / 32f;
        private const float INV_FACTOR = 1f / CONVERSION_FACTOR;

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

        private const int MAX_POLY = 256; // Max vector3 when looking up a path (for straight paths too).

        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static Dictionary<ushort, IntPtr> _navmeshPtrs = [];
        private static readonly Lock _navmeshPtrsLock = new();
        private static ThreadLocal<Dictionary<ushort, NavMeshQuery>> _navmeshQueries = new(() => []);

        [LibraryImport("dol_detour", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LoadNavMesh(string file, ref IntPtr meshPtr);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool FreeNavMesh(IntPtr meshPtr);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateNavMeshQuery(IntPtr meshPtr, ref IntPtr queryPtr);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool FreeNavMeshQuery(IntPtr queryPtr);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus PathStraight(
            IntPtr queryPtr,
            [In] float[] start,
            [In] float[] end,
            [In] float[] polyPickExt,
            [In] EDtPolyFlags[] queryFilter,
            EDtStraightPathOptions pathOptions,
            out int pointCount,
            [Out] float[] pointBuffer,
            [Out] EDtPolyFlags[] pointFlags);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus FindRandomPointAroundCircle(
            IntPtr queryPtr,
            [In] float[] center,
            float radius,
            [In] float[] polyPickExt,
            [In] EDtPolyFlags[] queryFilter,
            [Out] float[] outputVector);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus FindClosestPoint(
            IntPtr queryPtr,
            [In] float[] center,
            [In] float[] polyPickExt,
            [In] EDtPolyFlags[] queryFilter,
            [Out] float[] outputVector);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus GetPolyAt(
            IntPtr queryPtr,
            [In] float[] center,
            [In] float[] polyPickExt,
            [In] EDtPolyFlags[] queryFilter,
            out uint outputPolyRef,
            [Out] float[] outputVector);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus SetPolyFlags(IntPtr meshPtr, uint polyRef, EDtPolyFlags flags);

        [LibraryImport("dol_detour")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        private static partial EDtStatus QueryPolygons(IntPtr queryPtr,
            [In] float[] center,
            [In] float[] polyPickExt,
            [In] EDtPolyFlags[] queryFilter,
            [Out] uint[] outputPolyRefs,
            out int outputPolyCount,
            int maxPolyCount);

        [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr LoadLibrary(string dllName);

        [LibraryImport("libdl.so", StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr dlopen(string file, int mode);

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

        public bool Init()
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (LoadLibrary("lib\\dol_detour.dll") != IntPtr.Zero)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("dol_detour.dll loaded from LoadLibrary \"lib\\dol_detour.dll\"");
                    }
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (dlopen("lib/libdol_detour.so", 2 /* RTLD_NOW */) != IntPtr.Zero)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("libdol_detour.so loaded from dlopen \"lib/libdol_detour.so\"");
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                return false;
            }

            try
            {
                nint dummy = IntPtr.Zero;
                LoadNavMesh("this file does not exists!", ref dummy);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("PathingMgr did not find the dol_detour.dll", e);

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

        private static float[] ToRecastFloats(Vector3 value)
        {
            return [value.X * CONVERSION_FACTOR, value.Z * CONVERSION_FACTOR, value.Y * CONVERSION_FACTOR];
        }

        public WrappedPathingResult GetPathStraightAsync(Zone zone, Vector3 start, Vector3 end)
        {
            if (!_navmeshPtrs.TryGetValue(zone.ID, out nint ptr))
                return new WrappedPathingResult(EPathingError.NoPathFound, []);

            if (!_navmeshQueries.Value.TryGetValue(zone.ID, out NavMeshQuery query))
            {
                query = new NavMeshQuery(ptr);
                _navmeshQueries.Value.Add(zone.ID, query);
            }

            float[] startFloats = ToRecastFloats(start + Vector3.UnitZ * 8);
            float[] endFloats = ToRecastFloats(end + Vector3.UnitZ * 8);

            int numNodes = 0;
            float[] buffer = new float[MAX_POLY * 3];
            EDtPolyFlags[] flags = new EDtPolyFlags[MAX_POLY];
            EDtPolyFlags includeFilter = EDtPolyFlags.ALL ^ EDtPolyFlags.DISABLED;
            EDtPolyFlags excludeFilter = 0;
            float[] polyExt = ToRecastFloats(new Vector3(64, 64, 256));
            EDtStraightPathOptions options = EDtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS;
            EDtPolyFlags[] filter = [includeFilter, excludeFilter];
            EDtStatus status = PathStraight(query, startFloats, endFloats, polyExt, filter, options, out numNodes, buffer, flags);

            if ((status & EDtStatus.DT_SUCCESS) == 0)
                return new WrappedPathingResult(EPathingError.NoPathFound, []);

            WrappedPathPoint[] points = new WrappedPathPoint[numNodes];
            Vector3[] positions = Vector3ArrayFromRecastFloats(buffer, numNodes);

            for (int i = 0; i < numNodes; i++)
                points[i] = new(positions[i], flags[i]);

            return new WrappedPathingResult(EPathingError.PartialPathFound, points);
        }

        public Vector3? GetRandomPointAsync(Zone zone, Vector3 position, float radius)
        {
            if (!_navmeshPtrs.TryGetValue(zone.ID, out nint ptr))
                return null;

            Vector3? result = null;

            if (!_navmeshQueries.Value.TryGetValue(zone.ID, out NavMeshQuery query))
            {
                query = new NavMeshQuery(ptr);
                _navmeshQueries.Value.Add(zone.ID, query);
            }

            float[] center = ToRecastFloats(position + Vector3.UnitZ * 8);
            float cRadius = radius * CONVERSION_FACTOR;
            float[] outVec = new float[3];
            EDtPolyFlags defaultInclude = EDtPolyFlags.ALL ^ EDtPolyFlags.DISABLED;
            EDtPolyFlags defaultExclude = 0;
            EDtPolyFlags[] filter = [defaultInclude, defaultExclude];
            float[] polyPickEx = [2.0f, 4.0f, 2.0f];
            EDtStatus status = FindRandomPointAroundCircle(query, center, cRadius, polyPickEx, filter, outVec);

            if ((status & EDtStatus.DT_SUCCESS) != 0)
                result = new Vector3(outVec[0] * INV_FACTOR, outVec[2] * INV_FACTOR, outVec[1] * INV_FACTOR);

            return result;
        }

        public Vector3? GetClosestPointAsync(Zone zone, Vector3 position, float xRange = 256f, float yRange = 256f, float zRange = 256f)
        {
            // Assume the point is safe if we don't have a navmesh.
            if (!_navmeshPtrs.TryGetValue(zone.ID, out nint ptr))
                return position; 

            Vector3? result = null;

            if (!_navmeshQueries.Value.TryGetValue(zone.ID, out NavMeshQuery query))
            {
                query = new NavMeshQuery(ptr);
                _navmeshQueries.Value.Add(zone.ID, query);
            }

            float[] center = ToRecastFloats(position + Vector3.UnitZ * 8);
            float[] outVec = new float[3];
            EDtPolyFlags defaultInclude = EDtPolyFlags.ALL ^ EDtPolyFlags.DISABLED;
            EDtPolyFlags defaultExclude = 0;
            EDtPolyFlags[] filter = [defaultInclude, defaultExclude];
            float[] polyPickEx = ToRecastFloats(new Vector3(xRange, yRange, zRange));
            EDtStatus status = FindClosestPoint(query, center, polyPickEx, filter, outVec);

            if ((status & EDtStatus.DT_SUCCESS) != 0)
                result = new Vector3(outVec[0] * INV_FACTOR, outVec[2] * INV_FACTOR, outVec[1] * INV_FACTOR);

            return result;
        }

        private static Vector3[] Vector3ArrayFromRecastFloats(float[] buffer, int numNodes)
        {
            Vector3[] result = new Vector3[numNodes];

            for (int i = 0; i < numNodes; i++)
                result[i] = new Vector3(buffer[i * 3 + 0] * INV_FACTOR, buffer[i * 3 + 2] * INV_FACTOR, buffer[i * 3 + 1] * INV_FACTOR);

            return result;
        }

        public bool HasNavmesh(Zone zone)
        {
            return zone != null && _navmeshPtrs.ContainsKey(zone.ID);
        }

        public bool IsAvailable => true;
    }
}
