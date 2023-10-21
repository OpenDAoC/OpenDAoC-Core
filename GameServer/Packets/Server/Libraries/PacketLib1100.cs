using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1100, GameClient.eClientVersion.Version1100)]
	public class PacketLib1100 : PacketLib199
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.100 / 200 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1100(GameClient client)
			: base(client)
		{
			// Graveen: dumb support. Report if buggy.
		}
	}
}
