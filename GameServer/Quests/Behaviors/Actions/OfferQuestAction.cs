using System;
using Core.Events;
using Core.GS.Behaviors;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Quests.Actions
{
    [Action(ActionType = EActionType.OfferQuest)]
    public class OfferQuestAction : AAction<Type,String>
    {               

        public OfferQuestAction(GameNpc defaultNPC, EActionType actionType, Object p, Object q)
            : base(defaultNPC, EActionType.OfferQuest, p, q)
        {                
        }

        public OfferQuestAction(GameNpc defaultNPC, Type questType, String offerMessage)
            : this(defaultNPC, EActionType.OfferQuest, (object)questType, (object)offerMessage) { }
        

        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            string message = BehaviorUtil.GetPersonalizedMessage(Q, player);
            QuestMgr.ProposeQuestToPlayer(P, message, player, NPC);
        }
    }
}