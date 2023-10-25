using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI;

#region Council Otrygg
public class CouncilOtryggBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public CouncilOtryggBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 2000;
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
                {
                    if (npc == null) continue;
                    if (!npc.IsAlive) continue;
                    if (npc.Brain is OtryggAddBrain)
                    {
                        npc.Die(Body);
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (OtryggAdd.PetsCount is < 8 and >= 0)
            {
                SpawnPetsMore();
            }
        }
        base.Think();
    }
    public static bool IsPulled = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            SpawnPets();
            IsPulled = true;
        }
        base.OnAttackedByEnemy(ad);
    }
    public void SpawnPets()
    {
        for (int i = 0; i < 8; i++) // Spawn 8 pets
        {
            OtryggAdd Add = new OtryggAdd();
            Add.X = Body.SpawnPoint.X + Util.Random(-50, 80);
            Add.Y = Body.SpawnPoint.Y + Util.Random(-50, 80);
            Add.Z = Body.SpawnPoint.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
            OtryggAdd.PetsCount++;
        }
    }
    public void SpawnPetsMore()
    {
        OtryggAdd Add = new OtryggAdd();
        Add.X = Body.SpawnPoint.X + Util.Random(-50, 80);
        Add.Y = Body.SpawnPoint.Y + Util.Random(-50, 80);
        Add.Z = Body.SpawnPoint.Z;
        Add.CurrentRegion = Body.CurrentRegion;
        Add.Heading = Body.Heading;
        Add.AddToWorld();
        OtryggAdd.PetsCount++;
    }
}
#endregion Council Otrygg

#region Otrygg adds
public class OtryggAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public OtryggAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1600; //big aggro range
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
            {
                if (npc == null) continue;
                if (!npc.IsAlive) continue;
                if (npc.Brain is OtryggAddBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && Body != npc && Body.Brain != npc.Brain && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 10); //if one pet aggro all will aggro
                }
            }
        }
        base.Think();
    }
}
#endregion Otrygg adds