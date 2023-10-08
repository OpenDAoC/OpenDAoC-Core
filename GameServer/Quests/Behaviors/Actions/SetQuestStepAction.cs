using System;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Quests.Actions
{
    [Action(ActionType = EActionType.SetQuestStep)]
    public class SetQuestStepAction: AAction<Type,int>
    {

        public SetQuestStepAction(GameNPC defaultNPC, Object p, Object q)
            : base(defaultNPC, EActionType.SetQuestStep, p, q) 
        {
        }

        public SetQuestStepAction(GameNPC defaultNPC, Type questType, int questStep)
            : this(defaultNPC, (object)questType, (object)questStep)
        {
        }


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            AQuest playerQuest = player.IsDoingQuest(P) as AQuest;
            if (playerQuest != null)
            {
                playerQuest.Step = Q;                
            }
        }
    }
}