using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using DOL.GS.PacketHandler;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.Animation, IsNullableQ = true)]
    public class AnimationAction : AAction<EEmote,GameLiving>
    {               

        public AnimationAction(GameNPC defaultNPC, Object p, Object q)
            : base(defaultNPC, EActionType.Animation, p, q) { }
        

        public AnimationAction(GameNPC defaultNPC, EEmote emote, GameLiving actor)
            : this(defaultNPC, (object) emote, (object)actor) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

            GameLiving actor = Q != null ? Q : player;

            foreach (GamePlayer nearPlayer in actor.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                nearPlayer.Out.SendEmoteAnimation(actor, P);
            }
            
        }
    }
}