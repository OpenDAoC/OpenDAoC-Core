using System.Buffers;
using System.Numerics;

namespace OpenDAoC.Pathing
{
    // Game-space query wrapper over a native dtNavMeshQuery.
    // Coordinates are DAoC game units (X/Y horizontal, Z up).
    public sealed class DetourNavMeshQuery : IDisposable
    {
        public const int MAX_POLY = 256;
        private IntPtr _query;
        private bool _disposed;

        private DetourNavMeshQuery(DetourNavMesh mesh, IntPtr query)
        {
            Mesh = mesh;
            _query = query;
        }

        public IntPtr Handle
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _query;
            }
        }

        public DetourNavMesh Mesh { get; }

        internal static DetourNavMeshQuery Create(DetourNavMesh mesh)
        {
            IntPtr query = IntPtr.Zero;

            if (!DetourNative.CreateNavMeshQuery(mesh.Handle, ref query) || query == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create navmesh query.");

            return new(mesh, query);
        }

        public PathQueryResult PathStraight(
            Vector3 start,
            Vector3 end,
            Vector3 halfExtents,
            ReadOnlySpan<EDtPolyFlags> filters,
            EDtStraightPathOptions options,
            Span<Vector3> outPositions,
            Span<EDtPolyFlags> outFlags)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> startFloats = stackalloc float[3];
            Span<float> endFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            RecastCoords.FillRecastFloats(start, startFloats);
            RecastCoords.FillRecastFloats(end, endFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            float[] rentedBuffer = ArrayPool<float>.Shared.Rent(MAX_POLY * 3);
            EDtPolyFlags[] rentedFlags = ArrayPool<EDtPolyFlags>.Shared.Rent(MAX_POLY);

            try
            {
                Span<float> buffer = rentedBuffer.AsSpan(0, MAX_POLY * 3);
                Span<EDtPolyFlags> flags = rentedFlags.AsSpan(0, MAX_POLY);

                EDtStatus status = DetourNative.PathStraight(
                    _query, startFloats, endFloats, extents, filters, options,
                    out int pointCount, buffer, flags);

                if (!status.Succeeded())
                    return new(status, 0);

                if (outPositions.Length < pointCount || outFlags.Length < pointCount)
                    return new(status | EDtStatus.DT_BUFFER_TOO_SMALL, pointCount);

                for (int i = 0; i < pointCount; i++)
                {
                    outPositions[i] = RecastCoords.FromRecastFloats(buffer.Slice(i * 3, 3));
                    outFlags[i] = flags[i];
                }

                return new(status, pointCount);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(rentedBuffer);
                ArrayPool<EDtPolyFlags>.Shared.Return(rentedFlags);
            }
        }

        public Vector3? MoveAlongSurface(Vector3 start, Vector3 end, Vector3 halfExtents, ReadOnlySpan<EDtPolyFlags> filters)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> startFloats = stackalloc float[3];
            Span<float> endFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            Span<float> outVec = stackalloc float[3];
            RecastCoords.FillRecastFloats(start, startFloats);
            RecastCoords.FillRecastFloats(end, endFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            EDtStatus status = DetourNative.MoveAlongSurface(_query, startFloats, endFloats, extents, filters, outVec);
            return status.Succeeded() ? RecastCoords.FromRecastFloats(outVec) : null;
        }

        public Vector3? FindRandomPointAroundCircle(Vector3 center, float radius, Vector3 halfExtents, ReadOnlySpan<EDtPolyFlags> filters)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> centerFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            Span<float> outVec = stackalloc float[3];
            RecastCoords.FillRecastFloats(center, centerFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            EDtStatus status = DetourNative.FindRandomPointAroundCircle(
                _query, centerFloats, radius * RecastCoords.ConversionFactor, extents, filters, outVec);

            return status.Succeeded() ? RecastCoords.FromRecastFloats(outVec) : null;
        }

        public Vector3? FindClosestPoint(Vector3 center, Vector3 halfExtents, ReadOnlySpan<EDtPolyFlags> filters)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> centerFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            Span<float> outVec = stackalloc float[3];
            RecastCoords.FillRecastFloats(center, centerFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            EDtStatus status = DetourNative.FindClosestPoint(_query, centerFloats, extents, filters, outVec);
            return status.Succeeded() ? RecastCoords.FromRecastFloats(outVec) : null;
        }

        public Vector3? FindClosestPointInBox(
            Vector3 boxCenter,
            Vector3 boxExtents,
            Vector3 referencePos,
            ReadOnlySpan<EDtPolyFlags> filters)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> centerArr = stackalloc float[3];
            Span<float> extentsArr = stackalloc float[3];
            Span<float> refPosArr = stackalloc float[3];
            Span<float> outVec = stackalloc float[3];
            RecastCoords.FillRecastFloats(boxCenter, centerArr);
            RecastCoords.FillRecastFloats(boxExtents, extentsArr);
            RecastCoords.FillRecastFloats(referencePos, refPosArr);

            EDtStatus status = DetourNative.FindClosestPointInBox(
                _query, centerArr, extentsArr, refPosArr, filters, outVec);

            return status.Succeeded() ? RecastCoords.FromRecastFloats(outVec) : null;
        }

        public Vector3? FindClosestPointInBounds(
            Vector3 origin,
            Vector3 minOffset,
            Vector3 maxOffset,
            ReadOnlySpan<EDtPolyFlags> filters)
        {
            if (minOffset.X > maxOffset.X || minOffset.Y > maxOffset.Y || minOffset.Z > maxOffset.Z)
                throw new ArgumentException("minOffset must be <= maxOffset in all components");

            Vector3 relativeCenter = (minOffset + maxOffset) * 0.5f;
            Vector3 worldCenter = origin + relativeCenter;
            Vector3 extents = (maxOffset - minOffset) * 0.5f;
            return FindClosestPointInBox(worldCenter, extents, origin, filters);
        }

        public bool HasLineOfSight(Vector3 start, Vector3 end, Vector3 halfExtents, ReadOnlySpan<EDtPolyFlags> filters)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> startFloats = stackalloc float[3];
            Span<float> endFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            Span<float> outVec = stackalloc float[3];
            RecastCoords.FillRecastFloats(start, startFloats);
            RecastCoords.FillRecastFloats(end, endFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            EDtStatus status = DetourNative.HasLineOfSight(
                _query, startFloats, endFloats, extents, filters, out bool hasLos, outVec);

            return status.Succeeded() && hasLos;
        }

        public bool TryGetPolyAt(
            Vector3 center,
            Vector3 halfExtents,
            ReadOnlySpan<EDtPolyFlags> filters,
            out ulong polyRef,
            out Vector3 point)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> centerFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            Span<float> outVec = stackalloc float[3];
            RecastCoords.FillRecastFloats(center, centerFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            EDtStatus status = DetourNative.GetPolyAt(_query, centerFloats, extents, filters, out polyRef, outVec);

            if (!status.Succeeded() || polyRef == 0)
            {
                point = default;
                polyRef = 0;
                return false;
            }

            point = RecastCoords.FromRecastFloats(outVec);
            return true;
        }

        public int GetPolysInBox(
            Vector3 center,
            Vector3 halfExtents,
            ReadOnlySpan<EDtPolyFlags> filters,
            Span<ulong> polyRefs)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Span<float> centerFloats = stackalloc float[3];
            Span<float> extents = stackalloc float[3];
            RecastCoords.FillRecastFloats(center, centerFloats);
            RecastCoords.FillRecastFloats(halfExtents, extents);

            EDtStatus status = DetourNative.GetPolysInBox(
                _query, centerFloats, extents, filters, polyRefs, out int count, polyRefs.Length);

            if (!status.Succeeded() || count <= 0)
                return 0;

            return count;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_query != IntPtr.Zero)
            {
                DetourNative.FreeNavMeshQuery(_query);
                _query = IntPtr.Zero;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    public readonly record struct PathQueryResult(EDtStatus Status, int PointCount)
    {
        public bool Success => Status.Succeeded();
        public bool Partial => Status.IsPartial();
        public bool BufferTooSmall => (Status & EDtStatus.DT_BUFFER_TOO_SMALL) != 0;
    }
}
