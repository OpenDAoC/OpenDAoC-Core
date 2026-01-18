using DOL.Database;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.DestroyItemRequest, "Handles destroy item requests from client", eClientStatus.PlayerInGame)]
	public class DestroyItemRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			packet.Skip(4);
			int slot = packet.ReadShort();
			DbInventoryItem item = client.Player.Inventory.GetItem((eInventorySlot)slot);
			if (item != null)
			{
				if (item.IsIndestructible)
				{
					client.Out.SendMessage($"You can't destroy {item.GetName(0, false)}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				if (item.Id_nb == "ARelic")
				{
					client.Out.SendMessage("You cannot destroy a relic!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				if (client.Player.Inventory.EquippedItems.Contains(item))
				{
					client.Out.SendMessage("You cannot destroy an equipped item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				if (client.Player.Inventory.RemoveItem(item))
				{
					client.Out.SendMessage($"You destroy the {item.Name}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					InventoryLogging.LogInventoryAction(client.Player, "(destroy)", eInventoryActionType.Other, item.Template, item.Count);
				}
			}
		}
	}
}
