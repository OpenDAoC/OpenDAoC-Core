using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.TakeGold)]
    public class TakeGoldAction : AAction<long,Unused>
    {               

        public TakeGoldAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.TakeGold, p, q)
        {                
        }


        public TakeGoldAction(GameNPC defaultNPC, long p)
            : this(defaultNPC, (object)p, (object)null) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            player.RemoveMoney(P);
            InventoryLogging.LogInventoryAction(player, NPC, EInventoryActionType.Quest, P);
        }
    }
}