using System;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Fornfrusenen
public class FornfrusenenBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public FornfrusenenBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 2000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    private bool SpamMessage = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if(ad != null && ad.Attacker != null && ad.Attacker.IsAlive && !SpamMessage)
        {
            BroadcastMessage(String.Format("{0} awakens from its peaceful slumber and emerges from this ice walls and hisses \"I know your name {1}, take a good look at your surroundings! Within this ice is where you'll be entombed for all eternity! Hahahahaha\"", Body.Name, ad.Attacker.Name));
            SpamMessage = true;            
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
            FornInCombat = false;
            SpamMessage = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null && npc.IsAlive)
                    {
                        if (npc.Brain is FornShardBrain)
                            npc.RemoveFromWorld(); //remove adds here
                    }
                }
                RemoveAdds = true;
            }
        }

        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (FornInCombat == false)
            {
                SpawnShards(); //spawn adds here
                FornInCombat = true;
            }
        }
        base.Think();
    }

    public static bool FornInCombat = false;
    public void SpawnShards()
    {
        for (int i = 0; i < Util.Random(2, 3); i++)
        {
            FornfrusenenShard Add = new FornfrusenenShard();
            Add.X = Body.X + Util.Random(-100, 100);
            Add.Y = Body.Y + Util.Random(-100, 100);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }
    }
}
#endregion Fornfrusenen

#region Fornfrusenen Shard
public class FornShardBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public FornShardBrain()
        : base()
    {
        AggroLevel = 100; 
        AggroRange = 800;
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            Point3D point = new Point3D(49617, 32874, 10859);
            GameLiving target = Body.TargetObject as GameLiving;
            if (target != null && target.IsAlive)
            {
                if (!target.IsWithinRadius(point, 400) && !Body.IsWithinRadius(point, 400))
                    Body.MaxSpeedBase = 0;
                if (target.IsWithinRadius(point, 400))
                    Body.MaxSpeedBase = 200;
            }
        }
        base.Think();
    }
}
#endregion Fornfrusenen Shard