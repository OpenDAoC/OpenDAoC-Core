using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Behaviour.Attributes;

namespace Core.GS.Quests.Actions
{
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
}