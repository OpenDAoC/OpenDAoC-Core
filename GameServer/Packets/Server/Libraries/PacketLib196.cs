using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(196, EClientVersion.Version196)]
public class PacketLib196 : PacketLib195
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs a new PacketLib for Version 1.96 clients
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib196(GameClient client)
		: base(client)
	{
	}
}