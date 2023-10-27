using System;
using Core.GS.Behaviors;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Quests;

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