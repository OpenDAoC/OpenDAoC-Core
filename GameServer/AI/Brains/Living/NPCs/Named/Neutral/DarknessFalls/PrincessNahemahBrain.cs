using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class PrincessNahemahBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static bool spawnMinions = true;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            spawnMinions = true;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is NahemahMinionBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
            RemoveAdds = false;
        base.Think();
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (spawnMinions)
        {
            Spawn(); // spawn minions
            spawnMinions = false; // check to avoid spawning adds multiple times

            foreach (GameNpc mob_c in Body.GetNPCsInRadius(2000))
            {
                if (mob_c?.Brain is NahemahMinionBrain && mob_c.IsAlive && mob_c.IsAvailable)
                {
                    AddAggroListTo(mob_c.Brain as NahemahMinionBrain);
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    private void Spawn()
    {
        foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
        {
            if (npc.Brain is NahemahMinionBrain)
            {
                return;
            }
        }
        var amount = Util.Random(5, 10);
        for (int i = 0; i < amount; i++) // Spawn x minions
        {
            NahemahMinion Add = new NahemahMinion();
            Add.X = Body.X + Util.Random(100, 350);
            Add.Y = Body.Y + Util.Random(100, 350);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.IsWorthReward = false;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }
        spawnMinions = false;
    }
}

public class NahemahMinionBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public NahemahMinionBrain()
    {
        AggroLevel = 100;
        AggroRange = 450;
    }
}