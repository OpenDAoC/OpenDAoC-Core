using Core.GS;

namespace Core.AI.Brain;

#region Orshom
public class OrshomBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public OrshomBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
        CanBAF = false;
    }
    private bool Spawn_Fire = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            Spawn_Fire = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is OrshomFireBrain)
                        {
                            npc.RemoveFromWorld();
                            OrshomFire.FireCount = 0;
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (Spawn_Fire == false)
            {
                SpawnFire();
                Spawn_Fire = true;
            }
            if(Body.HealthPercent <=50)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is OrylleBrain brain)
                        {
                            if(!brain.HasAggro)
                                AddAggroListTo(brain);
                        }
                    }
                }
            }
        }
        base.Think();
    }
    public void SpawnFire()
    {
        foreach (GameNpc mob in Body.GetNPCsInRadius(8000))
        {
            if (mob.Brain is OrshomFireBrain)
            {
                return;
            }
        }
        OrshomFire npc = new OrshomFire();
        npc.X = 31406;
        npc.Y = 69599;
        npc.Z = 15605;
        npc.Heading = 2150;
        npc.CurrentRegion = Body.CurrentRegion;
        npc.RespawnInterval = -1;
        npc.AddToWorld();
    }
}
#endregion Orshom

#region Fire Pit Mob
public class OrshomFireBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public OrshomFireBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 2500;
        ThinkInterval = 1500;
    }

    public int AggroRange { get; set; }

    public override void Think()
    {
        base.Think();
    }
}
#endregion Fire Pit Mob