using System;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Quests.Actions
{
    [Action(ActionType = EActionType.IncQuestStep)]
    public class IncreaseQuestStepAction: AAction<Type,Unused>
    {

        public IncreaseQuestStepAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.IncQuestStep, p, q) 
        {        
        }

        public IncreaseQuestStepAction(GameNPC defaultNPC, Type questType)
            : this(defaultNPC, (object)questType, (object)null)
        { }


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            AQuest playerQuest = player.IsDoingQuest(P);
            if (playerQuest != null)
            {
                playerQuest.Step = playerQuest.Step + 1;
            }
        }
    }
}