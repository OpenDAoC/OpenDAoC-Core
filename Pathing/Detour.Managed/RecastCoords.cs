using System.Numerics;

namespace OpenDAoC.Pathing
{
    // Game-space (X/Y horizontal, Z up) ↔ Recast/Detour float layout (X, Z, Y) with 1/32 scale.
    public static class RecastCoords
    {
        public const float ConversionFactor = 1.0f / 32f;
        public const float InvFactor = 1f / ConversionFactor;

        // Default poly-pick half extents in game units (matches server pathfinding).
        public static readonly Vector3 DefaultHalfExtents = new(32f, 32f, 64f);

        public static void FillRecastFloats(Vector3 value, Span<float> destination)
        {
            destination[0] = value.X * ConversionFactor;
            destination[1] = value.Z * ConversionFactor;
            destination[2] = value.Y * ConversionFactor;
        }

        public static float[] GetRecastFloats(Vector3 source)
        {
            return
            [
                source.X * ConversionFactor,
                source.Z * ConversionFactor,
                source.Y * ConversionFactor,
            ];
        }

        public static Vector3 FromRecastFloats(ReadOnlySpan<float> recast)
        {
            return new(recast[0] * InvFactor, recast[2] * InvFactor, recast[1] * InvFactor);
        }

        public static Vector3 FromRecastFloats(float x, float y, float z)
        {
            return new(x * InvFactor, z * InvFactor, y * InvFactor);
        }
    }
}
