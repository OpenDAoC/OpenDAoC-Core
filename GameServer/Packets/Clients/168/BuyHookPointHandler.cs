using Core.GS.Keeps;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.BuyHookPoint, "buy hookpoint siege weapon/mob", EClientStatus.PlayerInGame)]
	public class BuyHookPointHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			ushort keepId = packet.ReadShort();
			ushort wallId = packet.ReadShort();
			int hookpointID = packet.ReadShort();
			ushort itemslot = packet.ReadShort();
			int payType = packet.ReadByte();//gold RP BP contrat???
			packet.ReadByte();
			packet.ReadByte();
			packet.ReadByte();


			AGameKeep keep = GameServer.KeepManager.GetKeepByID(keepId);
			if (keep == null)
				return;
			GameKeepComponent component = keep.KeepComponents[wallId] as GameKeepComponent;
			if (component == null)
				return;

			HookPointInventory inventory = null;
			if (hookpointID > 0x80) inventory = HookPointInventory.YellowHPInventory; // oil
			else if (hookpointID > 0x60) inventory = HookPointInventory.GreenHPInventory; // big siege
			else if (hookpointID > 0x40) inventory = HookPointInventory.LightGreenHPInventory; // small siege
			else if (hookpointID > 0x20) inventory = HookPointInventory.BlueHPInventory; // npc
			else inventory = HookPointInventory.RedHPInventory; // guard

			HookPointItem item = inventory?.GetItem(itemslot);
			item?.Invoke(client.Player, payType, component.HookPoints[hookpointID], component);
		}
	}
}