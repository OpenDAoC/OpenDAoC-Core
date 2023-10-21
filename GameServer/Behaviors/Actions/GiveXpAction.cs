using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.GiveXP)]
public class GiveXPAction : AAction<long,Unused>
{               

    public GiveXPAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.GiveXP, p, q) {                
    }


    public GiveXPAction(GameNpc defaultNPC, long p)
        : this(defaultNPC, (object)p,(object) null) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        player.GainExperience(EXpSource.NPC, P);
    }
}