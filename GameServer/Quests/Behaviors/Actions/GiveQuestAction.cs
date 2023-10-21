using System;
using Core.GS.Behaviors;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Quests;

[Action(ActionType = EActionType.GiveQuest)]
public class GiveQuestAction : AAction<Type,GameNpc>
{

    public GiveQuestAction(GameNpc defaultNPC, Object p, Object q)
        : base(defaultNPC, EActionType.GiveQuest, p, q) { }


    public GiveQuestAction(GameNpc defaultNPC, Type questType, GameNpc questGiver)
        : this(defaultNPC, (object)questType, (object)questGiver) { }
    

    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        QuestMgr.GiveQuestToPlayer(P, player, Q);            
    }
}