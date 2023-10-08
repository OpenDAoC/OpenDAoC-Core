using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(185, GameClient.eClientVersion.Version185)]
	public class PacketLib185 : PacketLib184
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.85 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib185(GameClient client):base(client)
		{
		}
	}
}
