using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(177, GameClient.eClientVersion.Version177)]
	public class PacketLib177 : PacketLib176
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.77 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib177(GameClient client):base(client)
		{
		}
	}
}
