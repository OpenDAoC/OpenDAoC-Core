using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace OpenDAoC.Pathing
{
    // P/Invoke surface for the native Detour library (DetourInterface.cpp).
    // Library resolution is handled by DetourNativeLibrary (absolute paths).
    public static partial class DetourNative
    {
        private const string LibraryName = DetourNativeLibrary.LibraryName;

        static DetourNative()
        {
            DetourNativeLibrary.EnsureResolverRegistered();
        }

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool LoadNavMesh(string file, ref IntPtr meshPtr);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FreeNavMesh(IntPtr meshPtr);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CreateNavMeshQuery(IntPtr meshPtr, ref IntPtr queryPtr);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FreeNavMeshQuery(IntPtr queryPtr);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus PathStraight(
            IntPtr queryPtr,
            ReadOnlySpan<float> start,
            ReadOnlySpan<float> end,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            EDtStraightPathOptions pathOptions,
            out int pointCount,
            Span<float> pointBuffer,
            Span<EDtPolyFlags> pointFlags);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus MoveAlongSurface(
            IntPtr query,
            ReadOnlySpan<float> start,
            ReadOnlySpan<float> end,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> filter,
            Span<float> outputVector);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus FindRandomPointAroundCircle(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            float radius,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            Span<float> outputVector);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus FindClosestPoint(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            Span<float> outputVector);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus FindClosestPointInBox(
            IntPtr queryPtr,
            ReadOnlySpan<float> boxCenter,
            ReadOnlySpan<float> boxExtents,
            ReadOnlySpan<float> referencePos,
            ReadOnlySpan<EDtPolyFlags> filter,
            Span<float> outputVector);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus HasLineOfSight(
            IntPtr query,
            ReadOnlySpan<float> start,
            ReadOnlySpan<float> end,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilters,
            [MarshalAs(UnmanagedType.I1)] out bool hasLos,
            Span<float> outputVector);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus UpdateFlags(
            IntPtr meshPtr,
            ReadOnlySpan<ulong> polyRefs,
            int polyCount,
            EDtPolyFlags flagsToRemove,
            EDtPolyFlags flagsToAdd);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus GetPolyAt(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            out ulong outputPolyRef,
            Span<float> outputVector);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial EDtStatus GetPolysInBox(
            IntPtr queryPtr,
            ReadOnlySpan<float> center,
            ReadOnlySpan<float> polyPickExt,
            ReadOnlySpan<EDtPolyFlags> queryFilter,
            Span<ulong> outputPolyRefs,
            out int outputPolyCount,
            int maxPolyCount);
    }
}
