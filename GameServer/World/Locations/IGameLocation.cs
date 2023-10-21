namespace DOL.GS
{
	/// <summary>
	/// interface for classes that represent a point in 3d space
	/// </summary>
	public interface IGameLocation : IPoint3D
	{
		ushort RegionID { get; }
		ushort Heading { get; }
		string Name { get; }
	}
}