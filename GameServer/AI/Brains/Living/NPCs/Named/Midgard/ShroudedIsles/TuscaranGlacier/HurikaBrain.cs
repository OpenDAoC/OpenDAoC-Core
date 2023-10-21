using System;
using System.Collections.Generic;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

public class HurikaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HurikaBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 2000;
    }

    public static bool point_1 = false;
    public static bool point_2 = false;
    public static bool point_3 = false;
    public static bool point_4 = false;
    public static bool point_5 = false;

    #region Hurika Flying Path
    public void HurikaFlyingPath()
    {
        Point3D point1 = new Point3D();
        point1.X = 54652;
        point1.Y = 36348;
        point1.Z = 18279;
        Point3D point2 = new Point3D();
        point2.X = 55113;
        point2.Y = 38549;
        point2.Z = 16679;
        Point3D point3 = new Point3D();
        point3.X = 53370;
        point3.Y = 40527;
        point3.Z = 16268;
        Point3D point4 = new Point3D();
        point4.X = 51711;
        point4.Y = 38978;
        point4.Z = 17130;
        Point3D point5 = new Point3D();
        point5.X = 51519;
        point5.Y = 37213;
        point5.Z = 17046;

        if (!Body.InCombat && !HasAggro)
        {
            if (Body.CurrentRegionID == 160) //tuscaran glacier
            {
                if (!Body.IsWithinRadius(point1, 30) && point_1 == false)
                {
                    Body.WalkTo(point1, 200);
                }
                else
                {
                    point_1 = true;
                    point_5 = false;
                    if (!Body.IsWithinRadius(point2, 30) && point_1 == true && point_2 == false)
                    {
                        Body.WalkTo(point2, 200);
                    }
                    else
                    {
                        point_2 = true;
                        if (!Body.IsWithinRadius(point3, 30) && point_1 == true && point_2 == true &&
                            point_3 == false)
                        {
                            Body.WalkTo(point3, 200);
                        }
                        else
                        {
                            point_3 = true;
                            if (!Body.IsWithinRadius(point4, 30) && point_1 == true && point_2 == true &&
                                point_3 == true && point_4 == false)
                            {
                                Body.WalkTo(point4, 200);
                            }
                            else
                            {
                                point_4 = true;
                                if (!Body.IsWithinRadius(point5, 30) && point_1 == true && point_2 == true &&
                                    point_3 == true && point_4 == true && point_5 == false)
                                {
                                    Body.WalkTo(point5, 200);
                                }
                                else
                                {
                                    point_5 = true;
                                    point_1 = false;
                                    point_2 = false;
                                    point_3 = false;
                                    point_4 = false;
                                }
                            }
                        }
                    }
                }
            }
            else //not TG
            {
                //mob will not roam
            }
        }
    }
    #endregion
    public List<GamePlayer> Port_Enemys = new List<GamePlayer>();
    public static bool IsTargetPicked = false;
    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Think()
    {
        HurikaFlyingPath();
        if (CheckProximityAggro() && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange) && Body.InCombat)
        {
            Body.Flags = 0; //dont fly
        }

        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            Body.Flags = ENpcFlags.FLYING; //fly
            IsTargetPicked = false;
            RandomTarget = null;
            if (Port_Enemys.Count > 0)
                Port_Enemys.Clear();
        }

        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if(HasAggro && Body.TargetObject != null)
        {
            foreach(GamePlayer player in Body.GetPlayersInRadius(1000))
            {
                if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !Port_Enemys.Contains(player))
                    Port_Enemys.Add(player);
            }
            if(Port_Enemys.Count > 0)
            {
                GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                RandomTarget = Target;
                if (RandomTarget.IsAlive && RandomTarget != null && !IsTargetPicked)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), Util.Random(15000, 20000));
                    IsTargetPicked = true;
                }
            }
        }

        base.Think();
    }
    private int TeleportPlayer(EcsGameTimer timer)
    {
        if (RandomTarget != null && RandomTarget.IsAlive && HasAggro && Body.IsAlive)
        {
            RandomTarget.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z + Util.Random(500, 700), Body.Heading);
            BroadcastMessage(String.Format("A powerful gust of wind generated by Hurika's wings sends {0} flying into the air!", RandomTarget.Name));
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetPort), 3500);
        return 0;
    }
    private int ResetPort(EcsGameTimer timer)
    {
        RandomTarget = null;//reset random target to null
        IsTargetPicked = false;
        return 0;
    }
}