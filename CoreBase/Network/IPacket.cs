namespace DOL.Network
{
	/// <summary>
	/// Defines the base functionality all packet wrappers must have.
	/// </summary>
	public interface IPacket
	{
		/// <summary>
		/// Generates a human-readable dump of the packet contents.
		/// </summary>
		/// <returns>a string representing the packet contents in hexadecimal</returns>
		string ToHumanReadable();
	}
}