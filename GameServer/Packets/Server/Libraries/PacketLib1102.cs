using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1102, GameClient.eClientVersion.Version1102)]
	public class PacketLib1102 : PacketLib1101
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.102
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1102(GameClient client)
			: base(client)
		{
			// Tolakram: dumb support, untested. Report if buggy.
		}
	}
}
