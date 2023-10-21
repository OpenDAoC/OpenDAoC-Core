using DOL.GS;

namespace DOL.AI.Brain;

#region Fuladl
public class FuladlBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FuladlBrain() : base()
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
                        if (npc.IsAlive && npc.Brain is FuladlAddBrain)
                        {
                            npc.RemoveFromWorld();
                            FuladlAdd.PartsCount = 0;
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
            FuladlAdd npc = new FuladlAdd();
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
#endregion Fuladl

#region Fuladl adds
public class FuladlAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FuladlAddBrain() : base()
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
#endregion Fuladl adds