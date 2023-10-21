using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.AI.Brain;

public class PrinceAsmoienBrain : StandardMobBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public PrinceAsmoienBrain()
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
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        else
        {
            foreach (GameNpc pet in Body.GetNPCsInRadius(2000))
            {
                if (pet.Brain is not IControlledBrain) continue;
                Body.Health += pet.MaxHealth;
                pet.Emote(EEmote.SpellGoBoom);
                pet.Die(Body);
            }
        }

        base.Think();
    }
}