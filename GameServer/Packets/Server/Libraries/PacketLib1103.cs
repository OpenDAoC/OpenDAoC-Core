using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(1103, EClientVersion.Version1103)]
public class PacketLib1103 : PacketLib1102
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs a new PacketLib for Client Version 1.103
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib1103(GameClient client)
		: base(client)
	{
		// Tolakram: dumb support, untested. Report if buggy.
	}
}