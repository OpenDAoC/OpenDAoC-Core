using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(178, EClientVersion.Version178)]
public class PacketLib178 : PacketLib177
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs a new PacketLib for Version 1.78 clients
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib178(GameClient client):base(client)
	{
	}
}