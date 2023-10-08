using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    // NOTE it is important that we look into the database for the npc because since it's not spawn at the moment the WorldMgr cant find it!!!
    [Action(ActionType = EActionType.MonsterSpawn,DefaultValueP=EDefaultValueConstants.NPC)]
    public class MonsterSpawnAction : AAction<GameLiving,Unused>
    {               

        public MonsterSpawnAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.MonsterSpawn, p, q)
        {                
        }


        public MonsterSpawnAction(GameNPC defaultNPC,  GameLiving npcToSpawn)
            : this(defaultNPC,  (object)npcToSpawn, (object)null) { }

        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {            
            if (P.AddToWorld())
            {
                // appear with a big buff of magic
                foreach (GamePlayer visPlayer in P.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    visPlayer.Out.SendSpellCastAnimation(P, 1, 20);
                }
                
            }
        }
    }
}