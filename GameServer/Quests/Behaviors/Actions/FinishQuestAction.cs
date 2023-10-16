using System;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Quests.Actions
{
    [Action(ActionType = EActionType.FinishQuest)]
    public class FinishQuestAction: AAction<Type,Unused>
    {

        public FinishQuestAction(GameNpc defaultNPC, Object p, Object q)
            : base(defaultNPC, EActionType.FinishQuest, p, q) 
        { }

        public FinishQuestAction(GameNpc defaultNPC, Type questType)
            : this(defaultNPC, (object) questType,(object) null)
        { }


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            AQuest playerQuest = player.IsDoingQuest(P);
            if (playerQuest != null)
                playerQuest.FinishQuest();
        }
    }
}