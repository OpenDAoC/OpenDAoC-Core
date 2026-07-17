using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenDAoC.Pathing
{
    internal static class DetourNativeLibrary
    {
        public const string LibraryName = "lib/Detour";

        private static int _resolverRegistered;

        internal static void EnsureResolverRegistered()
        {
            if (Interlocked.Exchange(ref _resolverRegistered, 1) != 0)
                return;

            NativeLibrary.SetDllImportResolver(typeof(DetourNativeLibrary).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!IsDetourLibraryName(libraryName))
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

        private static bool IsDetourLibraryName(string libraryName)
        {
            if (string.IsNullOrEmpty(libraryName))
                return false;

            // LibraryImport name, bare name, or platform-suffixed variants.
            string name = libraryName.Replace('\\', '/');
            return name.Equals(LibraryName, StringComparison.OrdinalIgnoreCase)
                   || name.Equals("Detour", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("lib/Detour.dll", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith("/Detour", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith("/Detour.dll", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Candidate absolute paths, most specific first.
        /// </summary>
        internal static IEnumerable<string> EnumerateCandidatePaths()
        {
            string fileName = OperatingSystem.IsWindows() ? "Detour.dll" : "libDetour.so";

            string? cwd = TryGetFullPath(Environment.CurrentDirectory);
            string? baseDir = TryGetFullPath(AppContext.BaseDirectory);

            // cwd/lib: GameServer root and BuildNav when cwd is already base/.
            if (cwd != null)
                yield return Path.Combine(cwd, "lib", fileName);

            // App base/lib: default for many hosts.
            if (baseDir != null)
                yield return Path.Combine(baseDir, "lib", fileName);

            // App base/base/lib: BuildNav layout (exe next to base/, cwd often set to base/).
            if (baseDir != null)
                yield return Path.Combine(baseDir, "base", "lib", fileName);

            // Parent of cwd/lib: if someone runs from a nested folder.
            if (cwd != null)
            {
                string? parent = Directory.GetParent(cwd)?.FullName;
                if (parent != null)
                    yield return Path.Combine(parent, "lib", fileName);
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
