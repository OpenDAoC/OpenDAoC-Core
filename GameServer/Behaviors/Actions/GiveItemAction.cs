using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.GiveItem,IsNullableQ=true)]
public class GiveItemAction : AAction<DbItemTemplate,GameNpc>
{               

    public GiveItemAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.GiveItem, p, q)
    {                
    }


    public GiveItemAction(GameNpc defaultNPC,  DbItemTemplate itemTemplate, GameNpc itemGiver)
        : this(defaultNPC, (object) itemTemplate, (object)itemGiver) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
		DbInventoryItem inventoryItem = GameInventoryItem.Create(P as DbItemTemplate);

        if (Q == null)
        {

            if (!player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, inventoryItem))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.GiveItemAction.GiveButInventFull", inventoryItem.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);                    
            }
            else
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.GiveItemAction.YouReceiveItem", inventoryItem.GetName(0, false)), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
                InventoryLogging.LogInventoryAction(Q, player, EInventoryActionType.Quest, inventoryItem.Template, inventoryItem.Count);
            }
        }
        else
        {                
            player.ReceiveItem(Q, inventoryItem);
        }            
    }
}