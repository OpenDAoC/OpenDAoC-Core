using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.MonsterUnspawn,DefaultValueP=EDefaultValueConstants.NPC)]
public class MonsterDespawnAction : AAction<GameLiving,Unused>
{               

    public MonsterDespawnAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.MonsterUnspawn, p, q)
    {                
    }


    public MonsterDespawnAction(GameNpc defaultNPC,  GameLiving monsterToUnspawn)
        : this(defaultNPC, (object)monsterToUnspawn, (object)null) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        P.RemoveFromWorld();
    }
}