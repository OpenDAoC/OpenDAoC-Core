using Core.GS.ECS;

namespace Core.GS.AI.Brains;

#region Gneiss
public class GneissBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GneissBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    private bool CanSpawnAdd = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            CanSpawnAdd = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is GneissAddBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "GneissBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
            if (!CanSpawnAdd)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CreateAdd), Util.Random(60000, 120000));
                CanSpawnAdd = true;
            }
        }
        base.Think();
    }
    private int CreateAdd(EcsGameTimer timer)
    {
        if (HasAggro)
            SpawnAdd();
        CanSpawnAdd = false;
        return 0;
    }
    private void SpawnAdd()
    {
        foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
        {
            if (npc.Brain is GneissAddBrain)
                return;
        }
        GneissAdd tree = new GneissAdd();
        tree.X = Body.X + Util.Random(-200, 200);
        tree.Y = Body.Y + Util.Random(-200, 200);
        tree.Z = Body.Z;
        tree.Heading = Body.Heading;
        tree.CurrentRegion = Body.CurrentRegion;
        tree.AddToWorld();
    }
}
#endregion Gneiss

#region Gneiss add
public class GneissAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GneissAddBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Gneiss add