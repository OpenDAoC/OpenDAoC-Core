using System;
using System.Reflection;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Behaviour;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.Attack,IsNullableP=true)]
public class AttackAction : AAction<int?,GameNpc>
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public AttackAction(GameNpc defaultNPC, Object p, Object q)
        : base(defaultNPC, EActionType.Attack, p, q)
    {
    }


    public AttackAction(GameNpc defaultNPC, Nullable<Int32> aggroAmount, GameNpc attacker)
        : this(defaultNPC, (object)aggroAmount, (object)attacker) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        int aggroAmount = P.HasValue ? P.Value : player.Level << 1;
        GameNpc attacker = Q;

        if (attacker.Brain is IOldAggressiveBrain)
        {
            IOldAggressiveBrain brain = (IOldAggressiveBrain)attacker.Brain;
            brain.AddToAggroList(player, aggroAmount);                
        }
        else
        {
            if (log.IsWarnEnabled)
            log.Warn("Non agressive mob " + attacker.Name + " was order to attack player. This goes against the first directive and will not happen");                
        }
    }
}