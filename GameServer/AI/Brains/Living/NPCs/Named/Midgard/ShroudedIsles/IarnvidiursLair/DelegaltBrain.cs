using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.AI;

public class DelegaltBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public DelegaltBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public static bool IsPulled = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
        }
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            if (IsPulled == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "DelegaltBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);
                        }
                    }
                }
                IsPulled = true;
            }
        }
        base.Think();
    }
}