using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.ECS;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.WalkToSpawn,DefaultValueP=EDefaultValueConstants.NPC)]
public class WalkToSpawnAction : AAction<GameNpc,Unused>
{               

    public WalkToSpawnAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.WalkToSpawn, p, q)
    {                
    }


    public WalkToSpawnAction(GameNpc defaultNPC, GameNpc npc)
        : this(defaultNPC,  (object)npc, (object)null) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        P.ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
    }
}