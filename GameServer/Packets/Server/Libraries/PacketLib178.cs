using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(178, GameClient.eClientVersion.Version178)]
	public class PacketLib178 : PacketLib177
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.78 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib178(GameClient client):base(client)
		{
		}
	}
}
