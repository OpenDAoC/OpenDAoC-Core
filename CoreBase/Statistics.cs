namespace DOL
{
	/// <summary>
	/// Static class that holds various statistics about the server instance.
	/// </summary>
	public static partial class Statistics
	{
		/// <summary>
		/// The total number of bytes received.
		/// </summary>
		public static long BytesIn;

		/// <summary>
		/// The total number of bytes sent.
		/// </summary>
		public static long BytesOut;

		/// <summary>
		/// The total number of packets received.
		/// </summary>
		public static long PacketsIn;

		/// <summary>
		/// The total number of packets sent.
		/// </summary>
		public static long PacketsOut;
	}
}