using Core.GS.Enums;

namespace Core.GS.AI.Brains;

#region Queen Qunilaria
public class QueenQunilariaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public QueenQunilariaBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public void SpawnAdds()
    {
        for (int i = 0; i < 3; i++)
        {
            if (QunilariaAdd.MinionCount < 4)
            {
                QunilariaAdd Add1 = new QunilariaAdd();
                Add1.X = Body.X + Util.Random(-100, 100);
                Add1.Y = Body.Y + Util.Random(-100, 100);
                Add1.Z = Body.Z;
                Add1.CurrentRegion = Body.CurrentRegion;
                Add1.Heading = Body.Heading;
                Add1.RespawnInterval = -1;
                Add1.PackageID = "QunilariaCombatAdd";
                Add1.AddToWorld();
            }
        }
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if(Util.Chance(25))
        {
            SpawnAdds();
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
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is QunilariaAddBrain && npc.PackageID == "QunilariaCombatAdd")
                        {
                            npc.Die(npc);
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (Body.TargetObject != null && Body.IsAlive && HasAggro)
            RemoveAdds = false;
        base.Think();
    }
}
#endregion Queen Qunilaria

#region Queen adds
public class QunilariaAddBrain : StandardMobBrain
{
    public QunilariaAddBrain()
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
#endregion Queen adds