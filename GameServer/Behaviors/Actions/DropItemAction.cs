using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Languages;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.DropItem)]
public class DropItemAction : AAction<DbItemTemplate,Unused>
{               

    public DropItemAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.DropItem, p, q)
    {                
    }


    public DropItemAction(GameNpc defaultNPC, DbItemTemplate itemTemplate)
        : this(defaultNPC, (object) itemTemplate,(object) null) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
		DbInventoryItem inventoryItem = GameInventoryItem.Create(P as DbItemTemplate);

        player.CreateItemOnTheGround(inventoryItem);
        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.DropItemAction.DropsFrontYou", inventoryItem.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
    }
}