using System.Numerics;
using Core.GS.Enums;

namespace Core.GS.World;

public struct WrappedPathingResult
{
	public EPathingError Error;
	public WrappedPathPoint[] Points;
}

public struct WrappedPathPoint
{
	public Vector3 Position;
	public EDtPolyFlags Flags;

	public override string ToString()
	{
		return $"({Position}, {Flags})";
	}
}