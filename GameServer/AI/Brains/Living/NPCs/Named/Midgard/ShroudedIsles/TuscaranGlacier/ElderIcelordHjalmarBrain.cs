using System;
using Core.GS.ECS;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Elder Icelord Hjalmar
public class ElderIcelordHjalmarBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ElderIcelordHjalmarBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    public static bool message1 = false;
    public static bool message2 = false;
    public static bool AggroText = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        Point3D point = new Point3D(31088, 53870, 11886);
        if (Body.IsAlive)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(8000))
            {
                if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !message2 && player.IsWithinRadius(point, 400))
                    message2 = true;
            }
            if (message2 && !message1)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Announce), 200);
                message1 = true;
            }
        }
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160394);
            Body.Strength = npcTemplate.Strength;
            message2 = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4500))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is MorkimmaBrain)
                                npc.Die(Body);
                        }
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
            if (message2 == false)
            {
                BroadcastMessage(Body.Name + " bellows 'I am amazed that you have made it this far! I'm afraid that your journey ends here with all of your death, however, I will show you no mercy!'");
                BroadcastMessage(String.Format(Body.Name +" says, I have warned the Council that if we do not destroy those who threaten us before they destroy us, we will perish." +
                    " You deserve this fate more than I do. I will not mourn her death beyond the grave!"));
                message2 = true;
            }

            if (Body.TargetObject != null)
            {
                float angle = Body.TargetObject.GetAngle(Body);
                GameLiving living = Body.TargetObject as GameLiving;
                if (Util.Chance(100))
                {
                    if (angle >= 150 && angle < 210)
                    {
                        Body.Strength = 740;
                        Body.styleComponent.NextCombatStyle = ElderIcelordHjalmar.back_style;
                    }
                    else
                    {
                        Body.Strength = 600;
                        Body.styleComponent.NextCombatStyle = ElderIcelordHjalmar.taunt;
                    }
                }
            }
        }
        base.Think();
    }
    private int Announce(EcsGameTimer timer)
    {
        BroadcastMessage("An otherworldly howling sound suddenly becomes perceptible. The sound quickly grows louder but it is not accompanied by a word. Moments after it begins, the howling sound is gone, replace by the familiar noises of the slowly shifting glacier.");
        return 0;
    }
}
#endregion Elder Icelord Hjalmar

#region Hjalmar adds
public class MorkimmaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public MorkimmaBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }     
}
#endregion Hjalmar adds