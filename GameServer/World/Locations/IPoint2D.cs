namespace DOL.GS
{
	/// <summary>
	/// interface for classes that represent a point in 2d space
	/// </summary>
	public interface IPoint2D
	{
		/// <summary>
		/// X
		/// </summary>
		int X { get; set; }

		/// <summary>
		/// Y
		/// </summary>
		int Y { get; set; }

		ushort GetHeading(IPoint2D point);
		Point2D GetPointFromHeading(ushort heading, int distance);
		int GetDistance(IPoint2D point);
		void Clear();
	}
}