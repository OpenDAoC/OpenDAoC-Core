using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
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
}