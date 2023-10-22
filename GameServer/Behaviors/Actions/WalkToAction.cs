using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.World;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.WalkTo,DefaultValueQ=EDefaultValueConstants.NPC)]
public class WalkToAction : AAction<IPoint3D,GameNpc>
{

    public WalkToAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.WalkTo, p, q)
    {                
        }


    public WalkToAction(GameNpc defaultNPC,  IPoint3D destination, GameNpc npc)
        : this(defaultNPC, (object) destination,(object) npc) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        IPoint3D location = (P is IPoint3D) ? (IPoint3D)P : player;            

        Q.WalkTo(location, Q.CurrentSpeed);
        
    }
}