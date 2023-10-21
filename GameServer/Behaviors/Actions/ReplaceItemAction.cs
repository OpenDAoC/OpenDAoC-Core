using System;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.ReplaceItem)]
public class ReplaceItemAction : AAction<DbItemTemplate,DbItemTemplate>
{               

    public ReplaceItemAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.ReplaceItem, p, q)
    {                
    }


    public ReplaceItemAction(GameNpc defaultNPC,  DbItemTemplate oldItemTemplate, DbItemTemplate newItemTemplate)
        : this(defaultNPC, (object) oldItemTemplate,(object) newItemTemplate) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        DbItemTemplate oldItem = P;
        DbItemTemplate newItem = Q;

        //TODO: what about stacked items???
        if (player.Inventory.RemoveTemplate(oldItem.Id_nb, 1, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
        {
            InventoryLogging.LogInventoryAction(player, NPC, EInventoryActionType.Quest, oldItem, 1);
			DbInventoryItem inventoryItem = GameInventoryItem.Create(newItem);
            if (player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, inventoryItem))
                InventoryLogging.LogInventoryAction(NPC, player, EInventoryActionType.Quest, newItem, 1);
        }
    }
}