using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Packets.Server;
using Core.GS.World;
using log4net;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.PlayerMoveItem, "Handle Moving Items Request", EClientStatus.PlayerInGame)]
public class PlayerMoveItemRequestHandler : IPacketHandler
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		if (client.Player == null)
			return;

		ushort id = packet.ReadShort();
		ushort toClientSlot = packet.ReadShort();
		ushort fromClientSlot = packet.ReadShort();
		ushort itemCount = packet.ReadShort();

		//ChatUtil.SendDebugMessage(client, "GM: MoveItem; id=" + id.ToString() + " client fromSlot=" + fromClientSlot.ToString() + " client toSlot=" + toClientSlot.ToString() + " itemCount=" + itemCount.ToString());

		// If our toSlot is > 1000 then target is a game object (not a window) with an ObjectID of toSlot - 1000

		if (toClientSlot > 1000)
		{
			ushort objectID = (ushort)(toClientSlot - 1000);
			GameObject obj = WorldMgr.GetObjectByIDFromRegion(client.Player.CurrentRegionID, objectID);
			if (obj == null || obj.ObjectState != GameObject.eObjectState.Active)
			{
				client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
				client.Out.SendMessage("Invalid trade target. (" + objectID + ")", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			GamePlayer tradeTarget = obj as GamePlayer;
			// If our target is another player we set the tradetarget
			// trade permissions are done in GamePlayer
			if (tradeTarget != null)
			{
				if (tradeTarget.Client.ClientState != GameClient.eClientState.Playing)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					client.Out.SendMessage("Can't trade with inactive players.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				if (tradeTarget == client.Player)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					client.Out.SendMessage("You can't trade with yourself, silly!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				if (!GameServer.ServerRules.IsAllowedToTrade(client.Player, tradeTarget, false))
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}
			}

			// Is the item we want to move in our backpack?
			// we also allow drag'n drop from equipped to blacksmith
			if ((fromClientSlot >= (ushort)EInventorySlot.FirstBackpack && 
				 fromClientSlot <= (ushort)EInventorySlot.LastBackpack) || 
				(obj is Blacksmith && 
				 fromClientSlot >= (ushort)EInventorySlot.MinEquipable && 
				 fromClientSlot <= (ushort)EInventorySlot.MaxEquipable))
			{
				if (!obj.IsWithinRadius(client.Player, WorldMgr.GIVE_ITEM_DISTANCE))
				{
					// show too far away message
					if (obj is GamePlayer)
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerMoveItemRequestHandler.TooFarAway", client.Player.GetName((GamePlayer)obj)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					else
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerMoveItemRequestHandler.TooFarAway", obj.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}

					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				DbInventoryItem item = client.Player.Inventory.GetItem((EInventorySlot)fromClientSlot);
				if (item == null)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					client.Out.SendMessage("Null item (client slot# " + fromClientSlot + ").", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}

				if (obj is GameNpc == false || item.Count == 1)
				{
					// see if any event handlers will handle this move
					client.Player.Notify(GamePlayerEvent.GiveItem, client.Player, new GiveItemEventArgs(client.Player, obj, item));
				}

				//If the item has been removed by the event handlers, return;
				if (item == null || item.OwnerID == null)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				// if a player to a GM and item is not dropable then don't allow trade???? This seems wrong.
				if (client.Account.PrivLevel == (uint)EPrivLevel.Player && tradeTarget != null && tradeTarget.Client.Account.PrivLevel != (uint)EPrivLevel.Player)
				{
					if (!item.IsDropable && !(obj is GameNpc && (obj is Blacksmith || obj is RechargerNpc || (obj as GameNpc).CanTradeAnyItem)))
					{
						client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
						client.Out.SendMessage("You can not remove this item!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return;
					}
				}

				if (tradeTarget != null)
				{
					// This is a player trade, let trade code handle
					tradeTarget.ReceiveTradeItem(client.Player, item);
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				if (obj.ReceiveItem(client.Player, item))
				{
					// this object was expecting an item and handled it
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
				return;
			}

			//Is the "item" we want to move money? For Version 1.78+
			if (client.Version >= GameClient.eClientVersion.Version178 && 
				fromClientSlot >= (int)EInventorySlot.Mithril178 && 
				fromClientSlot <= (int)EInventorySlot.Copper178)
			{
				fromClientSlot -= EInventorySlot.Mithril178 - EInventorySlot.Mithril;
			}

			//Is the "item" we want to move money?
			if (fromClientSlot >= (ushort)EInventorySlot.Mithril && fromClientSlot <= (ushort)EInventorySlot.Copper)
			{
				int[] money = new int[5];
				money[fromClientSlot - (ushort)EInventorySlot.Mithril] = itemCount;
				long flatMoney = MoneyMgr.GetMoney(money[0], money[1], money[2], money[3], money[4]);

				if (client.Version >= GameClient.eClientVersion.Version178) // add it back for proper slot update...
				{
					fromClientSlot += EInventorySlot.Mithril178 - EInventorySlot.Mithril;
				}

				if (!obj.IsWithinRadius(client.Player, WorldMgr.GIVE_ITEM_DISTANCE))
				{
					// show too far away message
					if (obj is GamePlayer)
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerMoveItemRequestHandler.TooFarAway", client.Player.GetName((GamePlayer)obj)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					else
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerMoveItemRequestHandler.TooFarAway", obj.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}

					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				if (flatMoney > client.Player.GetCurrentMoney())
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				client.Player.Notify(GamePlayerEvent.GiveMoney, client.Player, new GiveMoneyEventArgs(client.Player, obj, flatMoney));

				if (tradeTarget != null)
				{
					tradeTarget.ReceiveTradeMoney(client.Player, flatMoney);
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				if (obj.ReceiveMoney(client.Player, flatMoney))
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}

				client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
				return;
			}

			client.Out.SendInventoryItemsUpdate(null);
			return;
		}

		// We did not drop an item on a game object, which means we should have valid from and to slots 
		// since we are moving an item from one window to another.

		// First check for an active InventoryObject

		if (client.Player.ActiveInventoryObject != null && client.Player.ActiveInventoryObject.MoveItem(client.Player, fromClientSlot, toClientSlot))
		{
			//ChatUtil.SendDebugMessage(client, "ActiveInventoryObject handled move");
			return;
		}

		//Do we want to move an item from immediate inventory to immediate inventory or drop on the ground
		if (((fromClientSlot >= (ushort)EInventorySlot.Ground && fromClientSlot <= (ushort)EInventorySlot.LastBackpack)
			|| (fromClientSlot >= (ushort)EInventorySlot.FirstVault && fromClientSlot <= (ushort)EInventorySlot.LastVault)
			|| (fromClientSlot >= (ushort)EInventorySlot.FirstBagHorse && fromClientSlot <= (ushort)EInventorySlot.LastBagHorse))
			&& ((toClientSlot >= (ushort)EInventorySlot.Ground && toClientSlot <= (ushort)EInventorySlot.LastBackpack)
			|| (toClientSlot >= (ushort)EInventorySlot.FirstVault && toClientSlot <= (ushort)EInventorySlot.LastVault)
			|| (toClientSlot >= (ushort)EInventorySlot.FirstBagHorse && toClientSlot <= (ushort)EInventorySlot.LastBagHorse)))
		{
			//We want to drop the item
			if (toClientSlot == (ushort)EInventorySlot.Ground)
			{
				DbInventoryItem item = client.Player.Inventory.GetItem((EInventorySlot)fromClientSlot);
				if (item == null)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					client.Out.SendMessage("Invalid item (slot# " + fromClientSlot + ").", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				if (fromClientSlot < (ushort)EInventorySlot.FirstBackpack)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					return;
				}
				if (!item.IsDropable)
				{
					client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
					client.Out.SendMessage("You can not drop this item!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}

				if (client.Player.DropItem((EInventorySlot)fromClientSlot))
				{
					client.Out.SendMessage("You drop " + item.GetName(0, false) + " on the ground!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				client.Out.SendInventoryItemsUpdate(null);
				return;
			}

			client.Player.Inventory.MoveItem((EInventorySlot)fromClientSlot, (EInventorySlot)toClientSlot, itemCount);
			//ChatUtil.SendDebugMessage(client, "Player.Inventory handled move");
			return;
		}

		if (((fromClientSlot >= (ushort)EInventorySlot.Ground && fromClientSlot <= (ushort)EInventorySlot.LastBackpack)
			|| (fromClientSlot >= (ushort)EInventorySlot.FirstVault && fromClientSlot <= (ushort)EInventorySlot.LastVault)
			|| (fromClientSlot >= (ushort)EInventorySlot.FirstBagHorse && fromClientSlot <= (ushort)EInventorySlot.LastBagHorse))
			&& ((toClientSlot == (ushort)EInventorySlot.PlayerPaperDoll || toClientSlot == (ushort)EInventorySlot.NewPlayerPaperDoll)
			|| (toClientSlot >= (ushort)EInventorySlot.Ground && toClientSlot <= (ushort)EInventorySlot.LastBackpack)
			|| (toClientSlot >= (ushort)EInventorySlot.FirstVault && toClientSlot <= (ushort)EInventorySlot.LastVault)
			|| (toClientSlot >= (ushort)EInventorySlot.FirstBagHorse && toClientSlot <= (ushort)EInventorySlot.LastBagHorse)))
		{
			DbInventoryItem item = client.Player.Inventory.GetItem((EInventorySlot)fromClientSlot);
			if (item == null) return;

			toClientSlot = 0;
			if (item.Item_Type >= (int)EInventorySlot.MinEquipable && item.Item_Type <= (int)EInventorySlot.MaxEquipable)
				toClientSlot = (ushort)item.Item_Type;
			if (toClientSlot == 0)
			{
				client.Out.SendInventorySlotsUpdate(new int[] { fromClientSlot });
				return;
			}
			if (toClientSlot == (int)EInventorySlot.LeftBracer || toClientSlot == (int)EInventorySlot.RightBracer)
			{
				if (client.Player.Inventory.GetItem(EInventorySlot.LeftBracer) == null)
					toClientSlot = (int)EInventorySlot.LeftBracer;
				else
					toClientSlot = (int)EInventorySlot.RightBracer;
			}

			if (toClientSlot == (int)EInventorySlot.LeftRing || toClientSlot == (int)EInventorySlot.RightRing)
			{
				if (client.Player.Inventory.GetItem(EInventorySlot.LeftRing) == null)
					toClientSlot = (int)EInventorySlot.LeftRing;
				else
					toClientSlot = (int)EInventorySlot.RightRing;
			}

			client.Player.Inventory.MoveItem((EInventorySlot)fromClientSlot, (EInventorySlot)toClientSlot, itemCount);
			//ChatUtil.SendDebugMessage(client, "Player.Inventory handled move (2)");
			return;
		}

		client.Out.SendInventoryItemsUpdate(null);
	}
}