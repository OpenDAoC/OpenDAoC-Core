using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(1101, EClientVersion.Version1101)]
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