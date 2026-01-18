using DOL.Database;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.DuplicateNameCheck, "Checks if a character name already exists", eClientStatus.LoggedIn)]
	public class DupNameCheckRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			string name;
			if (client.Version >= GameClient.eClientVersion.Version1126)
				name = packet.ReadString(24);
			else
				name = packet.ReadString(30);

			var character = DOLDB<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(name));
			byte result = 0;
			// Bad Name check.
			if (character != null)
				result = 0x02;
			else if (GameServer.Instance.PlayerManager.InvalidNames[name])
				result = 0x01;

			client.Out.SendDupNameCheckReply(name, result);
		}
	}
}
