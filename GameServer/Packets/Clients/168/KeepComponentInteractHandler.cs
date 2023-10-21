using Core.GS.Keeps;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.KeepComponentInteract, "Keep component interact", EClientStatus.PlayerInGame)]
	public class KeepComponentInteractHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			ushort keepId = packet.ReadShort();
			ushort wallId = packet.ReadShort();
			ushort responce = packet.ReadShort();
			int HPindex = packet.ReadShort();

			AGameKeep keep = GameServer.KeepManager.GetKeepByID(keepId);

			if (keep == null || !(GameServer.ServerRules.IsSameRealm(client.Player, (GameKeepComponent)keep.KeepComponents[wallId], true) || client.Account.PrivLevel > 1))
				return;

			if (responce == 0x00)//show info
				client.Out.SendKeepComponentInteract(((GameKeepComponent)keep.KeepComponents[wallId]));
			else if (responce == 0x01)// click on hookpoint button
				client.Out.SendKeepComponentHookPoint(((GameKeepComponent)keep.KeepComponents[wallId]), HPindex);
			else if (responce == 0x02)//select an hookpoint
			{
				if (client.Account.PrivLevel > 1)
					client.Out.SendMessage("DEBUG : selected hookpoint id " + HPindex, EChatType.CT_Say, EChatLoc.CL_SystemWindow);

				GameKeepComponent hp = keep.KeepComponents[wallId];
				client.Out.SendClearKeepComponentHookPoint(hp, HPindex);
				client.Out.SendHookPointStore(hp.HookPoints[HPindex]);
			}
		}
	}
}
