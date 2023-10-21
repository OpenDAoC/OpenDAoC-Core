namespace Core.GS.AI.Brains;

public class DuraekEmpoweredBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public DuraekEmpoweredBrain() : base()
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
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
        }
        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            if(IsPulled==false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "DuraekBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
                        }
                    }
                }
                IsPulled = true;
            }
        }
        base.Think();
    }
}