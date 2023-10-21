using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Behaviour.Attributes;

namespace Core.GS.Quests.Actions
{
    [Action(ActionType = EActionType.IncQuestStep)]
    public class IncreaseQuestStepAction: AAction<Type,Unused>
    {

        public IncreaseQuestStepAction(GameNpc defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.IncQuestStep, p, q) 
        {        
        }

        public IncreaseQuestStepAction(GameNpc defaultNPC, Type questType)
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