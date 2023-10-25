using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.AI;

#region Abomos Soultrapper
public class AbomosSoultrapperBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public AbomosSoultrapperBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
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
                foreach (GameNpc adds in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (adds != null)
                    {
                        if (adds.IsAlive && adds.Brain is AbomosAddBrain)
                        {
                            adds.Die(adds);
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
            RemoveAdds = false;
        if(Body.InCombat && HasAggro && Body.HealthPercent <= 50)
        {
            SpawnAdds();
        }
        base.Think();
    }
    public void SpawnAdds()
    {
        for (int i = 0; i < 2; i++)
        {
            if (AbomosAdd.AddsCount < 3)
            {
                AbomosAdd Add1 = new AbomosAdd();
                Add1.X = Body.X;
                Add1.Y = Body.Y;
                Add1.Z = Body.Z;
                Add1.CurrentRegion = Body.CurrentRegion;
                Add1.Heading = Body.Heading;
                Add1.RespawnInterval = -1;
                Add1.AddToWorld();
            }
        }
    }
}
#endregion Abomos Soultrapper

#region Abomos adds
public class AbomosAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public AbomosAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
    }

    public override void Think()
    {
        base.Think();
    }
}
#endregion Abomos adds