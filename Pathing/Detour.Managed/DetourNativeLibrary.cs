using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenDAoC.Pathing
{
    internal static class DetourNativeLibrary
    {
        public const string LIBRARY_NAME = "lib/Detour";

        private static int _resolverRegistered;
        private readonly static string[] _candidateFileNames = OperatingSystem.IsWindows() ?
            ["Detour.dll"] :
            ["libDetour.so", "Detour.so"];

        internal static void EnsureResolverRegistered()
        {
            if (Interlocked.Exchange(ref _resolverRegistered, 1) != 0)
                return;

            NativeLibrary.SetDllImportResolver(typeof(DetourNativeLibrary).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!string.Equals(libraryName, LIBRARY_NAME, StringComparison.OrdinalIgnoreCase))
                return IntPtr.Zero;

            foreach (string candidate in EnumerateCandidatePaths())
            {
                if (!File.Exists(candidate))
                    continue;

                if (NativeLibrary.TryLoad(candidate, out IntPtr handle))
                    return handle;
            }

            // Let the default resolver try.
            return IntPtr.Zero;
        }

        internal static IEnumerable<string> EnumerateCandidatePaths()
        {
            string? cwd = TryGetFullPath(Environment.CurrentDirectory);
            string? baseDir = TryGetFullPath(AppContext.BaseDirectory);

            // cwd/lib: GameServer root and BuildNav when cwd is already base/.
            if (cwd != null)
            {
                foreach (string fileName in _candidateFileNames)
                    yield return Path.Combine(cwd, "lib", fileName);
            }

            // App base/lib: default for many hosts.
            if (baseDir != null)
            {
                foreach (string fileName in _candidateFileNames)
                    yield return Path.Combine(baseDir, "lib", fileName);
            }

            // App base/base/lib: BuildNav layout (exe next to base/, cwd often set to base/).
            if (baseDir != null)
            {
                foreach (string fileName in _candidateFileNames)
                    yield return Path.Combine(baseDir, "base", "lib", fileName);
            }

            // Parent of cwd/lib: if someone runs from a nested folder.
            if (cwd != null)
            {
                string? parent = Directory.GetParent(cwd)?.FullName;

                if (parent != null)
                {
                    foreach (string fileName in _candidateFileNames)
                        yield return Path.Combine(parent, "lib", fileName);
                }
            }

            // Optional override for packaging / CI.
            string? env = Environment.GetEnvironmentVariable("OPENDAOC_DETOUR_PATH");

            if (!string.IsNullOrWhiteSpace(env))
                yield return Path.GetFullPath(env);
        }

        private static string? TryGetFullPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        internal static bool TryLoad(out string? loadedPath, out Exception? error)
        {
            EnsureResolverRegistered();
            error = null;
            loadedPath = null;

            foreach (string candidate in EnumerateCandidatePaths())
            {
                if (!File.Exists(candidate))
                    continue;

                try
                {
                    if (NativeLibrary.TryLoad(candidate, out _))
                    {
                        loadedPath = candidate;
                        error = null;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            error ??= new DllNotFoundException($"Unable to load native Detour library. Searched: {string.Join(", ", EnumerateCandidatePaths())}");
            return false;
        }
    }
}
