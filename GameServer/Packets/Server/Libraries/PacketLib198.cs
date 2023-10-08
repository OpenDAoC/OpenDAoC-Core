using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(198, GameClient.eClientVersion.Version198)]
	public class PacketLib198 : PacketLib197
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.98 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib198(GameClient client)
			: base(client)
		{
			//Dunnerholl 2009-07-28: TODO CtoS_0x11 player market search got some new search parameters, they are simply appended as a few more bytes (12) need to do analysis and update handler
		}
	}
}
