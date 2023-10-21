using Core.GS;

namespace Core.AI.Brain;

public class WarlordDorinakkaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public WarlordDorinakkaBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }

        if (Body.TargetObject != null && HasAggro) //bring mobs from rooms if mobs got set PackageID="CryptLordBaf"
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.PackageID == "DorinakkaBaf")
                    {
                        AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                    }
                }
            }
        }
        base.Think();
    }
}