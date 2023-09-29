using System.Text;

namespace DOL
{ 
	public static class Constants
	{
		/// <summary>
		/// The size of the send buffer for a client socket.
		/// </summary>
		public const int SendBufferSize = 16 * 1024;

		/// <summary>
		/// The size of the receive buffer for a client socket.
		/// </summary>
		public const int ReceiveBufferSize = 16 * 1024;

		/// <summary>
		/// Whether or not to disable the Nagle algorithm. (TCP_NODELAY)
		/// </summary>
		public const bool UseNoDelay = true;

		/// <summary>
		/// The default encoding to use for all string operations in packet writing or reading.
		/// </summary>
		public static readonly Encoding DefaultEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);
	}
}