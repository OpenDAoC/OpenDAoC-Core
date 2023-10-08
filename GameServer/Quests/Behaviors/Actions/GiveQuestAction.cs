using System;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Quests.Actions
{
    [Action(ActionType = EActionType.GiveQuest)]
    public class GiveQuestAction : AAction<Type,GameNPC>
    {

        public GiveQuestAction(GameNPC defaultNPC, Object p, Object q)
            : base(defaultNPC, EActionType.GiveQuest, p, q) { }


        public GiveQuestAction(GameNPC defaultNPC, Type questType, GameNPC questGiver)
            : this(defaultNPC, (object)questType, (object)questGiver) { }
        

        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            QuestMgr.GiveQuestToPlayer(P, player, Q);            
        }
    }
}