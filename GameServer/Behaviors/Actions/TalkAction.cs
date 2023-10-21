using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.Talk,DefaultValueQ=EDefaultValueConstants.NPC)]
public class TalkAction : AAction<string,GameNpc>
{

    public TalkAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.Talk, p, q)
    {
    }


    public TalkAction(GameNpc defaultNPC, String message, GameNpc npc)
        : this(defaultNPC, (object)message, (object)npc) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        String message = BehaviorUtil.GetPersonalizedMessage(P, player);
        Q.TurnTo(player);
        Q.SayTo(player, message);
    }
}