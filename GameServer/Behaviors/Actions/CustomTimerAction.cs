using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.CustomTimer)]
    public class CustomTimerAction : AAction<EcsGameTimer,int>
    {

        public CustomTimerAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.CustomTimer, p, q)
        { 
            
        }


        public CustomTimerAction(GameNPC defaultNPC, EcsGameTimer gameTimer, int delay)
            : this(defaultNPC, (object) gameTimer,(object) delay) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            var timer = (EcsGameTimer)P;
            timer.Start(Q);
        }
    }
}