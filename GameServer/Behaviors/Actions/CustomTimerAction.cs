using System;
using Core.Events;
using Core.GS.Behaviour;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.CustomTimer)]
public class CustomTimerAction : AAction<EcsGameTimer,int>
{

    public CustomTimerAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.CustomTimer, p, q)
    { 
        
    }


    public CustomTimerAction(GameNpc defaultNPC, EcsGameTimer gameTimer, int delay)
        : this(defaultNPC, (object) gameTimer,(object) delay) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        var timer = (EcsGameTimer)P;
        timer.Start(Q);
    }
}