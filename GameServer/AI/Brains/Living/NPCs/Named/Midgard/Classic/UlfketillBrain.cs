using DOL.GS;

namespace DOL.AI.Brain;

#region Ulfketill
public class UlfketillBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public UlfketillBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (UlfketillAdds.JotunsCount < 3 && !HasAggro)
        {
            SpawnJotuns();
        }
        if(HasAggro && Body.TargetObject != null)
        {
            foreach(GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if(npc != null && npc.IsAlive && npc.Brain is UlfketillAddsBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
    private void SpawnJotuns()
    {
        for (int i = 0; i < 3; i++)
        {
            if (UlfketillAdds.JotunsCount < 3)
            {
                UlfketillAdds add = new UlfketillAdds();
                add.X = Body.X + Util.Random(-500, 500);
                add.Y = Body.Y + Util.Random(-500, 500);
                add.Z = Body.Z;
                add.Heading = Body.Heading;
                add.CurrentRegion = Body.CurrentRegion;
                add.AddToWorld();
            }
        }
    }
}
#endregion Ulfketill

#region Ulfketill adds
public class UlfketillAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
    public UlfketillAddsBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc != null && npc.IsAlive && npc.Brain is UlfketillBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        if(!HasAggro)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc != null && npc.IsAlive && npc.Brain is UlfketillBrain brain)
                {
                    if (!brain.HasAggro)
                        Body.Follow(npc, 150, 8000);
                }
            }
        }
        base.Think();
    }
}
#endregion Ulfketill adds