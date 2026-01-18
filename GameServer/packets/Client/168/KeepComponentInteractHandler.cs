using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.KeepComponentInteract, "Keep component interact", eClientStatus.PlayerInGame)]
	public class KeepComponentInteractHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			ushort keepId = packet.ReadShort();
			ushort wallId = packet.ReadShort();
			ushort responce = packet.ReadShort();
			int HPindex = packet.ReadShort();

			AbstractGameKeep keep = GameServer.KeepManager.GetKeepByID(keepId);

			if (keep == null || !(GameServer.ServerRules.IsSameRealm(client.Player, (GameKeepComponent)keep.KeepComponents[wallId], true) || client.Account.PrivLevel > 1))
				return;

			if (responce == 0x00)//show info
				client.Out.SendKeepComponentInteract(((GameKeepComponent)keep.KeepComponents[wallId]));
			else if (responce == 0x01)// click on hookpoint button
				client.Out.SendKeepComponentHookPoint(((GameKeepComponent)keep.KeepComponents[wallId]), HPindex);
			else if (responce == 0x02)//select an hookpoint
			{
				if (client.Account.PrivLevel > 1)
					client.Out.SendMessage("DEBUG : selected hookpoint id " + HPindex, eChatType.CT_Say, eChatLoc.CL_SystemWindow);

				GameKeepComponent hp = keep.KeepComponents[wallId];
				client.Out.SendClearKeepComponentHookPoint(hp, HPindex);
				client.Out.SendHookPointStore(hp.HookPoints[HPindex]);
			}
		}
	}
}
