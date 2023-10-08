using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(195, GameClient.eClientVersion.Version195)]
	public class PacketLib195 : PacketLib194
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.95 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib195(GameClient client)
			: base(client)
		{
		}
	}
}
