using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI.Brains;

#region Balere
public class BalereBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BalereBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    private bool spawnAdds = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            spawnAdds = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is BalereAddBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (!spawnAdds)
            {
                SpawnAdds();
                spawnAdds = true;
            }
        }
        base.Think();
    }
    private void SpawnAdds()
    {
        for (int i = 0; i < Util.Random(7, 8); i++)
        {
            BalereAdd add = new BalereAdd();
            add.X = Body.X + Util.Random(-300, 300);
            add.Y = Body.Y + Util.Random(-300, 300);
            add.Z = Body.Z;
            add.Heading = Body.Heading;
            add.CurrentRegion = Body.CurrentRegion;
            add.AddToWorld();
        }
    }
}
#endregion Balere

#region Balere adds
public class BalereAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BalereAddBrain() : base()
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
#endregion Balere adds