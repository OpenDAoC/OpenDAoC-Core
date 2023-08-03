using System;
using System.Collections.Generic;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;using DOL.GS.Behaviour;
using DOL.Database;

namespace DOL.GS.Behaviour.Actions
{
    [ActionAttribute(ActionType = EActionType.TakeGold)]
    public class TakeGoldAction : AbstractAction<long,Unused>
    {               

        public TakeGoldAction(GameNpc defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.TakeGold, p, q)
        {                
        }


        public TakeGoldAction(GameNpc defaultNPC, long p)
            : this(defaultNPC, (object)p, (object)null) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtils.GuessGamePlayerFromNotify(e, sender, args);
            player.RemoveMoney(P);
            InventoryLogging.LogInventoryAction(player, NPC, eInventoryActionType.Quest, P);
        }
    }
}