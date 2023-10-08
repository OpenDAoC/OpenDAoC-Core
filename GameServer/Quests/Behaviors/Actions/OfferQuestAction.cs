using System;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Quests.Actions
{
    [Action(ActionType = EActionType.OfferQuest)]
    public class OfferQuestAction : AAction<Type,String>
    {               

        public OfferQuestAction(GameNPC defaultNPC, EActionType actionType, Object p, Object q)
            : base(defaultNPC, EActionType.OfferQuest, p, q)
        {                
        }

        public OfferQuestAction(GameNPC defaultNPC, Type questType, String offerMessage)
            : this(defaultNPC, EActionType.OfferQuest, (object)questType, (object)offerMessage) { }
        

        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            string message = BehaviorUtil.GetPersonalizedMessage(Q, player);
            QuestMgr.ProposeQuestToPlayer(P, message, player, NPC);
        }
    }
}