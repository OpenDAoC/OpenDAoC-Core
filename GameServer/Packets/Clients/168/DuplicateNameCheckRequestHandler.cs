using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.DuplicateNameCheck, "Checks if a character name already exists", EClientStatus.LoggedIn)]
public class DupNameCheckRequestHandler : IPacketHandler
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		string name;
		if (client.Version >= EClientVersion.Version1126)
			name = packet.ReadString(24);
		else
			name = packet.ReadString(30);

		var character = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(name));
		byte result = 0;
		// Bad Name check.
		if (character != null)
			result = 0x02;
		else if (GameServer.Instance.PlayerManager.InvalidNames[name])
			result = 0x01;

		client.Out.SendDupNameCheckReply(name, result);
	}
}