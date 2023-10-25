using System;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.World;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.Animation, IsNullableQ = true)]
public class AnimationAction : AAction<EEmote,GameLiving>
{               

    public AnimationAction(GameNpc defaultNPC, Object p, Object q)
        : base(defaultNPC, EActionType.Animation, p, q) { }
    

    public AnimationAction(GameNpc defaultNPC, EEmote emote, GameLiving actor)
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