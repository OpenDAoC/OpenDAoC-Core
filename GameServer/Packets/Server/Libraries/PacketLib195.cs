using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(195, EClientVersion.Version195)]
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