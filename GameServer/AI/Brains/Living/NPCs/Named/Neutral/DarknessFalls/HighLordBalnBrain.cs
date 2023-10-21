using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using log4net;

namespace Core.GS.AI.Brains;

#region High Lord Baln
public class HighLordBalnBrain : StandardMobBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public HighLordBalnBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 850;
    }

    private bool RemoveAdds = false;
    private bool CanPopMinions = false;

    private int SpawnMinion(EcsGameTimer timer)
    {
        for (int i = 0; i < Util.Random(8, 12); i++)
        {
            BalnMinion sMinion = new BalnMinion();
            sMinion.X = Body.X + Util.Random(-100, 100);
            sMinion.Y = Body.Y + Util.Random(-100, 100);
            sMinion.Z = Body.Z;
            sMinion.CurrentRegion = Body.CurrentRegion;
            sMinion.Heading = Body.Heading;
            sMinion.AddToWorld();
        }

        CanPopMinions = false;
        return 0;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is BalnMinionBrain)
                            npc.Die(npc);
                    }
                }

                RemoveAdds = true;
            }
        }

        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (!CanPopMinions)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMinion), Util.Random(20000, 35000));
                CanPopMinions = true;
            }
        }

        base.Think();
    }
}
#endregion High Lord Baln

public class BalnMinionBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BalnMinionBrain()
    {
        AggroLevel = 100;
        AggroRange = 450;
    }
}