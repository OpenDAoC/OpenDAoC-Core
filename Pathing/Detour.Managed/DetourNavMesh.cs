namespace OpenDAoC.Pathing
{
    // Owns a loaded native dtNavMesh.
    public sealed class DetourNavMesh : IDisposable
    {
        private IntPtr _mesh;
        private bool _disposed;

        private DetourNavMesh(IntPtr mesh)
        {
            _mesh = mesh;
        }

        public IntPtr Handle
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _mesh;
            }
        }

        public static bool TryProbeNativeLibrary(out Exception? error)
        {
            if (!DetourNativeLibrary.TryLoad(out string? loadedPath, out Exception? loadError))
            {
                error = loadError ?? new DllNotFoundException("Unable to load native Detour library.");
                return false;
            }

            try
            {
                // Exercise a P/Invoke entry point so LibraryImport bindings are fully resolved.
                IntPtr dummy = IntPtr.Zero;
                DetourNative.LoadNavMesh("this file does not exist!", ref dummy);
                error = null;
                _ = loadedPath;
                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
        }

        public static DetourNavMesh? TryLoad(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            IntPtr meshPtr = IntPtr.Zero;

            if (!DetourNative.LoadNavMesh(Path.GetFullPath(path), ref meshPtr) || meshPtr == IntPtr.Zero)
                return null;

            return new(meshPtr);
        }

        public static DetourNavMesh Load(string path)
        {
            DetourNavMesh mesh = TryLoad(path) ?? throw new InvalidOperationException($"Failed to load navmesh from '{path}'.");
            return mesh;
        }

        public DetourNavMeshQuery CreateQuery()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return DetourNavMeshQuery.Create(this);
        }

        public EDtStatus UpdateFlags(ReadOnlySpan<ulong> polyRefs, EDtPolyFlags flagsToRemove, EDtPolyFlags flagsToAdd)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return polyRefs.IsEmpty ? EDtStatus.DT_SUCCESS : DetourNative.UpdateFlags(_mesh, polyRefs, polyRefs.Length, flagsToRemove, flagsToAdd);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_mesh != IntPtr.Zero)
            {
                DetourNative.FreeNavMesh(_mesh);
                _mesh = IntPtr.Zero;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
