using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class SarcondinaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public SarcondinaBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    private bool CanSpawnAdd = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            CanSpawnAdd = false;
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "SarcondinaAdd")
                    npc.Die(Body);
            }
        }
        if (HasAggro)
        {
            if (CanSpawnAdd == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdd), Util.Random(25000, 40000));
                CanSpawnAdd = true;
            }
        }
        base.Think();
    }
    private int SpawnAdd(EcsGameTimer timer)
    {
        if (HasAggro && Body.IsAlive)
        {
            GameNpc add = new GameNpc();
            add.Name = Body.Name + "'s servant";
            add.Model = 933;
            add.Size = (byte)Util.Random(45, 55);
            add.Level = (byte)Util.Random(55, 59);
            add.Strength = 120;
            add.Quickness = 80;
            add.MeleeDamageType = EDamageType.Crush;
            add.MaxSpeedBase = 225;
            add.PackageID = "SarcondinaAdd";
            add.RespawnInterval = -1;
            add.X = Body.X + Util.Random(-100, 100);
            add.Y = Body.Y + Util.Random(-100, 100);
            add.Z = Body.Z;
            add.CurrentRegion = Body.CurrentRegion;
            add.Heading = Body.Heading;
            add.Faction = FactionMgr.GetFactionByID(64);
            add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            StandardMobBrain brain = new StandardMobBrain();
            add.SetOwnBrain(brain);
            brain.AggroRange = 800;
            brain.AggroLevel = 100;
            add.AddToWorld();
        }
        CanSpawnAdd = false;
        return 0;
    }
}