using DOL.GS;

namespace DOL.AI.Brain;

public class AniliusBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public AniliusBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    private bool SpawnAdds = false;
    private bool RemoveAdds = false;

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.IsAlive && npc != null && npc.Brain is AniliusAddBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
            SpawnAdds = false;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (!SpawnAdds)
            {
                SpawnAniliusAdds();
                SpawnAdds = true;
            }
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.Brain is AniliusAddBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && target.IsAlive && target != null)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
    private void SpawnAniliusAdds()
    {
        for (int i = 0; i < 3; i++)
        {
            AniliusAdd npc = new AniliusAdd();
            npc.X = Body.X + Util.Random(-50, 50);
            npc.Y = Body.Y + Util.Random(-50, 50);
            npc.Z = Body.Z;
            npc.Heading = Body.Heading;
            npc.CurrentRegion = Body.CurrentRegion;
            npc.AddToWorld();
        }
    }
}

#region Anilius adds
public class AniliusAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public AniliusAddBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Anilius adds