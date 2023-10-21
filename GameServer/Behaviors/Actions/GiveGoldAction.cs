using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.GiveGold)]
public class GiveGoldAction : AAction<long,Unused>
{               

    public GiveGoldAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.GiveGold, p, q)
    {                
    }


    public GiveGoldAction(GameNpc defaultNPC, long p)
        : this(defaultNPC,  (object)p,(object) null) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        player.AddMoney(P);
        InventoryLogging.LogInventoryAction(NPC, player, EInventoryActionType.Quest, P);
    }
}