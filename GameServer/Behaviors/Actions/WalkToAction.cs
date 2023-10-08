using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.WalkTo,DefaultValueQ=EDefaultValueConstants.NPC)]
    public class WalkToAction : AAction<IPoint3D,GameNPC>
    {

        public WalkToAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.WalkTo, p, q)
        {                
            }


        public WalkToAction(GameNPC defaultNPC,  IPoint3D destination, GameNPC npc)
            : this(defaultNPC, (object) destination,(object) npc) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            IPoint3D location = (P is IPoint3D) ? (IPoint3D)P : player;            

            Q.WalkTo(location, Q.CurrentSpeed);
            
        }
    }
}