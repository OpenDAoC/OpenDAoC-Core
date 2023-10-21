using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(192, GameClient.eClientVersion.Version192)]
	public class PacketLib192 : PacketLib191
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.92 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib192(GameClient client)
			: base(client)
		{
		}
	}
}
