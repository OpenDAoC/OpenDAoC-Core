using System;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

public class EasmarachBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public EasmarachBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool restphase = false;
    public static bool dontattack = false;
    public override void AttackMostWanted()
    {
        if (dontattack==true)
            return;
        else
        {
            if (ECS.Debug.Diagnostics.AggroDebugEnabled)
            {
                PrintAggroTable();
            }
            Body.TargetObject = CalculateNextAttackTarget();
            if (Body.TargetObject != null)
            {
                if (!CheckSpells(ECheckSpellType.Offensive))
                {
                    Body.StartAttack(Body.TargetObject);
                }
            }
        }
        base.AttackMostWanted();
    }
    private int StartWalk(EcsGameTimer timer)
    {
        dontattack = true;
        return 0;
    }
    private int EndWalk(EcsGameTimer timer)
    {
        restphase = false;
        healBoss = false;
        return 0;
    }
    private bool healBoss = false;
    public override void Think()
    {
        Point3D point1 = new Point3D(37811, 50342, 10958);
        if (Body.HealthPercent <= 30 && !restphase)
        {
            ClearAggroList();
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(StartWalk), 100);
            BroadcastMessage(String.Format("{0} is retreating to waterfall!",Body.Name));
            restphase = true;
        }
        if (dontattack && !Body.IsWithinRadius(point1, 50) && restphase)
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
            Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            Body.Z = 10958;
            Body.WalkTo(point1, 200);
        }

        if (Body.IsWithinRadius(point1, 50) && !healBoss && restphase)
        {
            if(Body.HealthPercent <= 30)
                Body.Health += Body.MaxHealth / 8;

            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(EndWalk), Util.Random(50000,80000));
            dontattack = false;
            healBoss = true;
        }

        if (Body.IsAlive)
        {
            Point3D nopass = new Point3D(37653, 52843, 10758);//you shall not pass!
            foreach(GamePlayer player in Body.GetPlayersInRadius(10000))
            {
                if(player != null)
                {
                    if(player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (player.IsWithinRadius(nopass, 1000))
                        { 
                            player.MoveTo(Body.CurrentRegionID, 40067, 50494, 11708, 1066);
                            player.Out.SendMessage("The strong current of the waterfall pushes you behind", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                        }
                    }
                }
            }
        }
        if (Body.InCombatInLast(65 * 1000) == false && this.Body.InCombatInLast(70 * 1000))
        {
            Body.Health = Body.MaxHealth;
            restphase = false;
            dontattack = false;
            healBoss = false;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
            Body.MaxSpeedBase = npcTemplate.MaxSpeed;
        }
        if (Body.IsOutOfTetherRange && !dontattack && Body.TargetObject != null)
        {
            Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
            GameLiving target = Body.TargetObject as GameLiving;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
            if (target != null)
            {
                if (!target.IsWithinRadius(spawn, Body.TetherRange))
                {
                    Body.MaxSpeedBase = 0;
                }
                else
                    Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            }
        }
        base.Think();
    }
}