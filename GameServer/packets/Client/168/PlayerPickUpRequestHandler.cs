using DOL.Language;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PickUpRequest, "Handles Pick up object request", eClientStatus.PlayerInGame)]
	public class PlayerPickUpRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			if (client.Player == null)
				return;
			uint X = packet.ReadInt();
			uint Y = packet.ReadInt();
			ushort id = packet.ReadShort();
			ushort obj = packet.ReadShort();

			GameObject target = client.Player.TargetObject;
			if (target == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPickUpRequestHandler.HandlePacket.Target"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (target.ObjectState != GameObject.eObjectState.Active)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPickUpRequestHandler.HandlePacket.InvalidTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			client.Player.PickupObject(target, false);
		}
	}
}
