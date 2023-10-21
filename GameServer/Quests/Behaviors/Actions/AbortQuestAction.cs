using System;
using Core.Events;
using Core.GS.Behaviors;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Quests.Actions
{
    [Action(ActionType = EActionType.AbortQuest)]
    public class AbortQuestAction: AAction<Type,Unused>
    {

        public AbortQuestAction(GameNpc defaultNPC, Object p, Object q)
            : base(defaultNPC, EActionType.AbortQuest, p, q) 
        { }

        public AbortQuestAction(GameNpc defaultNPC, Type questType)
            : this(defaultNPC, (object)questType, (object)null)
        { }


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            AQuest playerQuest = player.IsDoingQuest(P);
            if (playerQuest != null)
            {
                playerQuest.AbortQuest();
            }
        }
    }
}