using System;
using System.Collections;
using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Icelord Steinvor
public class IcelordSteinvorBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public IcelordSteinvorBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public static bool IsPulled = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is IcelordSkufBrain)
                    {
                        AddAggroListTo(npc.Brain as IcelordSkufBrain);
                        IsPulled = true;
                    }
                }
            }
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
            IsPulled = false;
            PickedTarget = false;
        }

        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            if (!RemoveAdds)
            {
                foreach (GameNpc mob in Body.GetNPCsInRadius(5000))
                {
                    if (mob != null)
                    {
                        if (mob.IsAlive)
                        {
                            if (mob.Brain is EffectMobBrain)
                                mob.RemoveFromWorld();
                        }
                    }
                }
                RemoveAdds = true;
            }
        }

        if (Body.TargetObject != null && HasAggro)
        {
            RemoveAdds = false;
            if (PickedTarget == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickPlayer), 1000);
                PickedTarget = true;
            }
        }
        base.Think();
    }

    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    public static bool PickedTarget = false;
    public static int PlayerX = 0;
    public static int PlayerY = 0;
    public static int PlayerZ = 0;

    public int PickPlayer(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            IList enemies = new ArrayList(AggroTable.Keys);
            foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 1);
                    }
                }
            }
            if (enemies.Count == 0)
            {}
            else
            {
                List<GameLiving> damage_enemies = new List<GameLiving>();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    if (!(enemies[i] is GameLiving))
                        continue;
                    if (!(enemies[i] as GameLiving).IsAlive)
                        continue;
                    GameLiving living = null;
                    living = enemies[i] as GameLiving;
                    if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
                    {
                        damage_enemies.Add(enemies[i] as GameLiving);
                    }
                }
                if (damage_enemies.Count > 0)
                {
                    GamePlayer PortTarget = (GamePlayer)damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                    RandomTarget = PortTarget;
                    PlayerX = RandomTarget.X;
                    PlayerY = RandomTarget.Y;
                    PlayerZ = RandomTarget.Z;
                    SpawnEffectMob();
                    BroadcastMessage(String.Format(Body.Name + " says, '" + RandomTarget.Name +" you are not going anywhere'"));
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(EffectTimer), 8000);
                }
            }
        }
        return 0;
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public int EffectTimer(EcsGameTimer timer) //pick and remove effect mob
    {
        if (Body.IsAlive)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is EffectMobBrain)
                        npc.RemoveFromWorld();
                }
            }
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RestartEffectTimer), Util.Random(10000, 15000));
        }
        return 0;
    }

    public int RestartEffectTimer(EcsGameTimer timer) //reset timer so boss can repeat it again
    {
        if (Body.IsAlive)
        {
            PlayerX = 0;
            PlayerY = 0;
            PlayerZ = 0;
            RandomTarget = null;
            PickedTarget = false;
        }
        return 0;
    }
    public void SpawnEffectMob() //spawn mob to show effect on ground
    {
        EffectMob npc = new EffectMob();
        npc.X = PlayerX;
        npc.Y = PlayerY;
        npc.Z = PlayerZ;
        npc.RespawnInterval = -1;
        npc.Heading = Body.Heading;
        npc.CurrentRegion = Body.CurrentRegion;
        npc.AddToWorld();
    }
}
#endregion Icelord Steinvor

#region Mob adds
public class HrimthursaSeerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HrimthursaSeerBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 1000;
    }

    public static bool walkto_point = false;
    public void Walk_To_Room()
    {
        Point3D point1 = new Point3D();
        point1.X = 29986;
        point1.Y = 50345;
        point1.Z = 11377;

        if (!Body.InCombat && !HasAggro)
        {
            if (Body.CurrentRegionID == 160) //TG
            {
                if (!Body.IsWithinRadius(point1, 30) && walkto_point == false)
                    Body.WalkTo(point1, 100);
                else
                    walkto_point = true;
            }
        }
    }
    public override void Think()
    {
        Walk_To_Room();
        base.Think();
    }
}
#endregion Mob adds

#region Effect Mob
public class EffectMobBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public EffectMobBrain()
        : base()
    {
        AggroLevel = 0;
        AggroRange = 0;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (Body.IsAlive)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 100);
                    }
                }
            }
        }
        base.Think();
    }
}
#endregion Effect Mob