using System;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Quests.Actions
{
    [Action(ActionType = EActionType.OfferQuestAbort)]
    public class OfferQuestAbortAction : AAction<Type,String>
    {               

        public OfferQuestAbortAction(GameNPC defaultNPC, Object p, Object q)
            : base(defaultNPC, EActionType.OfferQuestAbort, p, q)
        {
        }


        public OfferQuestAbortAction(GameNPC defaultNPC, Type questType, String offerAbortMessage)
            : this(defaultNPC, (object)questType, (object)offerAbortMessage) { }
        

        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            string message = BehaviorUtil.GetPersonalizedMessage(Q, player);
            QuestMgr.AbortQuestToPlayer(P, message, player, NPC);
        }
    }
}