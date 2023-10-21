using System;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

#region Lord Sanguis
public class LordSanguisBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LordSanguisBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            BloodMage.MageCount = 0;
            if (!RemoveAdds)
            {
                foreach (GameNpc mages in Body.GetNPCsInRadius(5000))
                {
                    if (mages != null)
                    {
                        if (mages.IsAlive && mages.Brain is BloodMageBrain)
                            mages.RemoveFromWorld();
                    }
                }
                RemoveAdds= true;
            }
        }
        if (Body.TargetObject != null && HasAggro)
        {
            RemoveAdds = false;
            if (Util.Chance(10))
            {
                if (BloodMage.MageCount < 2)
                    SpawnMages();
            }
        }
        base.Think();
    }
    public void SpawnMages()
    {
        BloodMage Add = new BloodMage();
        Add.X = Body.X + Util.Random(-50, 80);
        Add.Y = Body.Y + Util.Random(-50, 80);
        Add.Z = Body.Z;
        Add.CurrentRegion = Body.CurrentRegion;
        Add.Heading = Body.Heading;
        Add.AddToWorld();
    }
}
#endregion Lord Sanguis

#region Lich Lord Sanguis
public class LichLordSanguisBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LichLordSanguisBrain()
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
    public static bool set_flag = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            Body.Flags = ENpcFlags.GHOST;
            set_flag = false;
        }
        if (Body.HealthPercent <= 5)
        {
            if (set_flag == false)
            {
                BroadcastMessage(String.Format(Body.Name + " becomes almost untouchable in his last act of agony!"));
                Body.Flags ^= ENpcFlags.CANTTARGET;
                set_flag = true;
            }
        }
        base.Think();
    }
}
#endregion Lich Lord Sanguis

#region Blood Mage
public class BloodMageBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BloodMageBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }

    public override void Think()
    {
        base.Think();
    }
}
#endregion Blood Mage