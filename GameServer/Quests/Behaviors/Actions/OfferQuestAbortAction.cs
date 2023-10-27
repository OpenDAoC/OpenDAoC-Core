using System;
using Core.GS.Behaviors;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Quests;

[Action(ActionType = EActionType.OfferQuestAbort)]
public class OfferQuestAbortAction : AAction<Type,String>
{               

    public OfferQuestAbortAction(GameNpc defaultNPC, Object p, Object q)
        : base(defaultNPC, EActionType.OfferQuestAbort, p, q)
    {
    }


    public OfferQuestAbortAction(GameNpc defaultNPC, Type questType, String offerAbortMessage)
        : this(defaultNPC, (object)questType, (object)offerAbortMessage) { }
    

    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        string message = BehaviorUtil.GetPersonalizedMessage(Q, player);
        QuestMgr.AbortQuestToPlayer(P, message, player, NPC);
    }
}