using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.MonsterUnspawn,DefaultValueP=EDefaultValueConstants.NPC)]
    public class MonsterDespawnAction : AAction<GameLiving,Unused>
    {               

        public MonsterDespawnAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.MonsterUnspawn, p, q)
        {                
        }


        public MonsterDespawnAction(GameNPC defaultNPC,  GameLiving monsterToUnspawn)
            : this(defaultNPC, (object)monsterToUnspawn, (object)null) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            P.RemoveFromWorld();
        }
    }
}