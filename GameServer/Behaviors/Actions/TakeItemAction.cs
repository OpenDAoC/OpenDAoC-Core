using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.TakeItem,DefaultValueQ=1)]
public class TakeItemAction : AAction<DbItemTemplate, int>
{

    public TakeItemAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.TakeItem, p, q)
    {
    }


    public TakeItemAction(GameNpc defaultNPC,   DbItemTemplate itemTemplate, int quantity)
        : this(defaultNPC, (object)itemTemplate,(object) quantity) { }



    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        int count = Q;
        DbItemTemplate itemToRemove = P;

		Dictionary<DbInventoryItem, int?> dataSlots = new Dictionary<DbInventoryItem, int?>(10);
        lock (player.Inventory)
        {
            var allBackpackItems = player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);

            bool result = false;
            foreach (DbInventoryItem item in allBackpackItems)
            {
                if (item.Name == itemToRemove.Name)
                {

                    if (item.IsStackable) // is the item is stackable
                    {
                        if (item.Count >= count)
                        {
                            if (item.Count == count)
                            {
                                dataSlots.Add(item, null);
                            }
                            else
                            {
                                dataSlots.Add(item, count);
                            }
                            result = true;
                            break;
                        }
                        else
                        {
                            dataSlots.Add(item, null);
                            count -= item.Count;
                        }
                    }
                    else
                    {
                        dataSlots.Add(item, null);
                        if (count <= 1)
                        {
                            result = true;
                            break;
                        }
                        else
                        {
                            count--;
                        }
                    }
                }
            }
            if (result == false)
            {
                return;
            }

            GamePlayerInventory playerInventory = player.Inventory as GamePlayerInventory;
            playerInventory.BeginChanges();
			Dictionary<DbInventoryItem, int?>.Enumerator enumerator = dataSlots.GetEnumerator();
            while (enumerator.MoveNext())
            {
	
	KeyValuePair<DbInventoryItem, int?> de = enumerator.Current;
                
	if (de.Value.HasValue)
                {
                    playerInventory.RemoveItem(de.Key);
                    InventoryLogging.LogInventoryAction(player, NPC, EInventoryActionType.Quest, de.Key.Template, de.Key.Count);
                }
                else
                {
                    playerInventory.RemoveCountFromStack(de.Key, de.Value.Value);
                    InventoryLogging.LogInventoryAction(player, NPC, EInventoryActionType.Quest, de.Key.Template, de.Value.Value);
                }
            }
            playerInventory.CommitChanges();

            if (NPC != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.TakeItemAction.YouGiveItemToNPC", itemToRemove.Name, NPC.GetName(0, false)), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
            }
            else
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.TakeItemAction.YouGiveItem", itemToRemove.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
            }
        }
    }
}