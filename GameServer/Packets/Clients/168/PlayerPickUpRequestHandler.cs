using DOL.Language;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.PickUpRequest, "Handles Pick up object request", EClientStatus.PlayerInGame)]
	public class PlayerPickUpRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
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
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPickUpRequestHandler.HandlePacket.Target"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (target.ObjectState != GameObject.eObjectState.Active)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPickUpRequestHandler.HandlePacket.InvalidTarget"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			client.Player.PickupObject(target, false);
		}
	}
}
