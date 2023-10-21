namespace Core.GS.AI.Brains;

public class DagarBrain : StandardMobBrain
{
    public DagarBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public static bool IsPulled = false;
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.PackageID == "DagarBaf")
                    {
                        AddAggroListTo(npc.Brain as StandardMobBrain);
                    }
                }
            }
        }
        base.Think();
    }
}