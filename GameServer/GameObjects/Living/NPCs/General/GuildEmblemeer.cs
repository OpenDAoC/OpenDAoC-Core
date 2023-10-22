using System;
using Core.Base;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Packets.Server;

namespace Core.GS;

[NpcGuildScript("Guild Emblemeer")]
public class GuildEmblemeer : GameNpc
{
	public const long EMBLEM_COST = 50000;
	private const string EMBLEMIZE_ITEM_WEAK = "emblemise item";

	/// <summary>
	/// Can accept any item
	/// </summary>
	public override bool CanTradeAnyItem
	{
		get { return true; }
	}

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		TurnTo(player, 5000);
		
		// Check for ambient trigger messages for the NPC in the 'MobXAmbientBehaviour' table
		var triggers = GameServer.Instance.NpcManager.AmbientBehaviour[base.Name];
		// If the NPC has no ambient trigger message assigned, then return this message
		if (triggers == null || triggers.Length == 0)
			SayTo(player, EChatLoc.CL_ChatWindow, "For 5 gold, I can put the emblem of your guild on the item. Just hand me the item.");

		return true;
	}

	public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
	{
		GamePlayer t = source as GamePlayer;
		if (t == null || item == null)
			return false;
		
		if (item.Emblem != 0)
		{
			t.Out.SendMessage("This item already has an emblem on it.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return false;
		}

		if (item.Object_Type == (int) EObjectType.Shield
			|| item.Item_Type == Slot.CLOAK)
		{
			if (t.Guild == null)
			{
				t.Out.SendMessage("You have no guild.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			if (t.Guild.Emblem == 0)
			{
				t.Out.SendMessage("Your guild has no emblem.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			if (t.Level < 20) //if level of player < 20 so can not put emblem
			{
				if (t.CraftingPrimarySkill == ECraftingSkill.NoCrafting)
				{
					t.Out.SendMessage("You have to be at least level 20 or have 400 in a tradeskill to be able to wear an emblem.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return false;
				}
				else
				{
					if (t.GetCraftingSkillValue(t.CraftingPrimarySkill) < 400)
					{
						t.Out.SendMessage("You have to be at least level 20 or have 400 in a tradeskill to be able to wear an emblem.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return false;
					}
				}

			}

			if (!t.Guild.HasRank(t, EGuildRank.Emblem))
			{
				t.Out.SendMessage("You do not have enough privileges for that.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			t.TempProperties.SetProperty(EMBLEMIZE_ITEM_WEAK, new WeakRef(item));
			t.Out.SendCustomDialog("Do you agree to put an emblem on this object?", new CustomDialogResponse(EmblemerDialogResponse));
		}
		else
			t.Out.SendMessage("I can not put an emblem on this item.", EChatType.CT_System, EChatLoc.CL_SystemWindow);

		return false;
	}

	protected void EmblemerDialogResponse(GamePlayer player, byte response)
	{
		WeakReference itemWeak = player.TempProperties.GetProperty<WeakReference>(EMBLEMIZE_ITEM_WEAK, new WeakRef(null));
		player.TempProperties.RemoveProperty(EMBLEMIZE_ITEM_WEAK);

		if (response != 0x01)
			return; //declined

		DbInventoryItem item = (DbInventoryItem) itemWeak.Target;

		if (item == null || item.SlotPosition == (int) EInventorySlot.Ground
			|| item.OwnerID == null || item.OwnerID != player.InternalID)
		{
			player.Out.SendMessage("Invalid item.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		if (!player.RemoveMoney(EMBLEM_COST))
		{
            InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, EMBLEM_COST);
			player.Out.SendMessage("You don't have enough money.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		item.Emblem = player.Guild.Emblem;
		player.Out.SendInventoryItemsUpdate(new DbInventoryItem[] {item});
		if (item.SlotPosition < (int) EInventorySlot.FirstBackpack)
			player.UpdateEquipmentAppearance();
		SayTo(player, EChatLoc.CL_ChatWindow, "I have put an emblem on your item.");
		return;
	}
}