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
			GameKeepComponent component = keep?.GetComponent(wallId);

			if (component == null || !(GameServer.ServerRules.IsSameRealm(client.Player, component, true) || client.Account.PrivLevel > 1))
				return;

			if (responce == 0x00)//show info
				client.Out.SendKeepComponentInteract(component);
			else if (responce == 0x01)// click on hookpoint button
				client.Out.SendKeepComponentHookPoint(component, HPindex);
			else if (responce == 0x02)//select an hookpoint
			{
				if (!component.HookPoints.TryGetValue(HPindex, out GameKeepHookPoint hookPoint))
					return;

				if (client.Account.PrivLevel > 1)
					client.Out.SendMessage("DEBUG : selected hookpoint id " + HPindex, eChatType.CT_Say, eChatLoc.CL_SystemWindow);

				client.Out.SendClearKeepComponentHookPoint(component, HPindex);
				client.Out.SendHookPointStore(hookPoint);
			}
		}
	}
}
