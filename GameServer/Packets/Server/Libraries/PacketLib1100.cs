using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(1100, EClientVersion.Version1100)]
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