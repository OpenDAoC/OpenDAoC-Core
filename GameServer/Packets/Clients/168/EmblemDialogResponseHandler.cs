using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.EmblemDialogResponse, "Handles when a player chooses a guild emblem", EClientStatus.PlayerInGame)]
	public class EmblemDialogResponseHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			if(client.Player.Guild == null)
				return;
			if(!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
				return;
			int primarycolor = packet.ReadByte() & 0x0F; //4bits
			int secondarycolor = packet.ReadByte() & 0x07; //3bits
			int pattern = packet.ReadByte() & 0x03; //2bits
			int logo = packet.ReadByte(); //8bits
			int oldemblem = client.Player.Guild.Emblem;
			int newemblem = ((logo << 9) | (pattern << 7) | (primarycolor << 3) | secondarycolor);
			if (GuildMgr.IsEmblemUsed(newemblem))
			{
				client.Player.Out.SendMessage("This emblem is already in use by another guild, please choose another!", EChatType.CT_System, EChatLoc.CL_SystemWindow );
				return;
			}
			GuildMgr.ChangeEmblem(client.Player, oldemblem, newemblem);
		}
	}
}
