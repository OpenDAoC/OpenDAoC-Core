using System;
using Core.GS.Behaviors;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Quests;

[Action(ActionType = EActionType.SetQuestStep)]
public class SetQuestStepAction: AAction<Type,int>
{

    public SetQuestStepAction(GameNpc defaultNPC, Object p, Object q)
        : base(defaultNPC, EActionType.SetQuestStep, p, q) 
    {
    }

    public SetQuestStepAction(GameNpc defaultNPC, Type questType, int questStep)
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