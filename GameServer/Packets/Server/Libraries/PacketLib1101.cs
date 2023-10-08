using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1101, GameClient.eClientVersion.Version1101)]
	public class PacketLib1101 : PacketLib1100
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.101
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1101(GameClient client)
			: base(client)
		{
			// Tolakram: dumb support, untested. Report if buggy.
		}
	}
}
