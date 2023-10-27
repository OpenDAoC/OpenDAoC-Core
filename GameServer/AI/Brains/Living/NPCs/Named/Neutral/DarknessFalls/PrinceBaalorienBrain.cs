using System.Reflection;
using Core.GS.Enums;
using Core.GS.World;
using log4net;

namespace Core.GS.AI;

public class PrinceBaalorienBrain : StandardMobBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public PrinceBaalorienBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 850;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        else
        {
            foreach (GameNpc pet in Body.GetNPCsInRadius(2000))
            {
                if (pet.Brain is not IControlledBrain) continue;
                Body.Health += pet.MaxHealth;
                foreach (GamePlayer player in pet.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(Body, pet, 368, 0, false, 1);
                pet.Die(Body);
            }
        }

        base.Think();
    }
}