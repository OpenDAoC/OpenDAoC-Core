namespace DOL.GS
{
	/// <summary>
	/// interface for classes that represent a point in 3d space
	/// </summary>
	public interface IPoint3D : IPoint2D
	{
		/// <summary>
		/// Height Position
		/// </summary>
		int Z { get; set; }
	}
}