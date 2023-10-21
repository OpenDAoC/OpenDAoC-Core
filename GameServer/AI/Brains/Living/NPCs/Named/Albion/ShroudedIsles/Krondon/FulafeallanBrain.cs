using DOL.GS;

namespace DOL.AI.Brain;

#region Fulafeallan
public class FulafeallanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FulafeallanBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    private bool spawnadds = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            spawnadds = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is FulafeallanAddBrain)
                        {
                            npc.RemoveFromWorld();
                            FulafeallanAdd.PartsCount2 = 0;
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (spawnadds == false)
            {
                SpawnAdds();
                spawnadds = true;
            }
        }
        base.Think();
    }
    public void SpawnAdds()
    {
        for (int i = 0; i < Util.Random(4, 5); i++)
        {
            FulafeallanAdd npc = new FulafeallanAdd();
            npc.X = Body.X + Util.Random(-100, 100);
            npc.Y = Body.Y + Util.Random(-100, 100);
            npc.Z = Body.Z;
            npc.Heading = Body.Heading;
            npc.CurrentRegion = Body.CurrentRegion;
            npc.RespawnInterval = -1;
            npc.AddToWorld();
        }
    }
}
#endregion Fulafeallan

#region Fulafeallan adds
public class FulafeallanAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FulafeallanAddBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Fulafeallan adds