using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(197, EClientVersion.Version197)]
public class PacketLib197 : PacketLib196
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs a new PacketLib for Version 1.97 clients
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib197(GameClient client)
		: base(client)
	{
	}
}