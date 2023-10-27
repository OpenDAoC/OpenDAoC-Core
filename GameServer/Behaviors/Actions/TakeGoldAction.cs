using System;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.TakeGold)]
public class TakeGoldAction : AAction<long,Unused>
{               

    public TakeGoldAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.TakeGold, p, q)
    {                
    }


    public TakeGoldAction(GameNpc defaultNPC, long p)
        : this(defaultNPC, (object)p, (object)null) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        player.RemoveMoney(P);
        InventoryLogging.LogInventoryAction(player, NPC, EInventoryActionType.Quest, P);
    }
}