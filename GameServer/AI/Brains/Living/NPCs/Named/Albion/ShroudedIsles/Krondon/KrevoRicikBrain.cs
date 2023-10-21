using Core.GS.Enums;

namespace Core.GS.AI.Brains;

#region Krevo Ricik
public class KrevoRicikBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public KrevoRicikBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if(ad != null && ad.Attacker is GamePlayer && ad.Damage > 0)
        {
            if(Util.Chance(10))
                ad.Attacker.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z + 400, Body.Heading);
            if (Util.Chance(15))
                SpawnGhost();
        }
        base.OnAttackedByEnemy(ad);
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            if (!RemoveAdds)
            {
                foreach (GameNpc add in Body.GetNPCsInRadius(4000))
                {
                    if (add == null) continue;
                    if (add.IsAlive && add.Brain is KrevoAddBrain)
                        add.Die(Body);
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
            RemoveAdds = false;
        base.Think();
    }
    public void SpawnGhost()
    {
        foreach (GameNpc add in Body.GetNPCsInRadius(4000))
        {
            if (add.Brain is KrevoAddBrain)
            {
                return;
            }
        }
        KrevolAdd npc = new KrevolAdd();
        npc.X = Body.X + Util.Random(-100, 100);
        npc.Y = Body.Y + Util.Random(-100, 100);
        npc.Z = Body.Z;
        npc.Heading = Body.Heading;
        npc.CurrentRegion = Body.CurrentRegion;
        npc.RespawnInterval = -1;
        npc.AddToWorld();
    }
}
#endregion Krevo Ricik

#region Krevo adds
public class KrevoAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public KrevoAddBrain() : base()
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
#endregion Krevo adds
