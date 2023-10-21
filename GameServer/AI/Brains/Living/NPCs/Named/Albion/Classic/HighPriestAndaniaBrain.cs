using System;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

public class HighPriestAndaniaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public HighPriestAndaniaBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 300;
        ThinkInterval = 1000;
    }
    ushort oldModel;
    ENpcFlags oldFlags;
    bool changed;
    bool playerInRoom = false;
    bool Message = false;

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(1500))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Think()
    {
        foreach(GamePlayer player in Body.GetPlayersInRadius(500))
        {
            if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                playerInRoom = true;
        }
        if (playerInRoom)
        {
            if (changed)
            {
                Body.Flags = oldFlags;
                Body.Model = oldModel;
                changed = false;
            }
        }
        else
        {
            if (changed == false)
            {
                oldFlags = Body.Flags;
                Body.Flags ^= ENpcFlags.CANTTARGET;
                Body.Flags ^= ENpcFlags.DONTSHOWNAME;
                Body.Flags ^= ENpcFlags.PEACE;

                if (oldModel == 0)
                    oldModel = Body.Model;

                Body.Model = 1;
                changed = true;
            }
        }
        if(!CheckProximityAggro())
            Message = false;

        if(HasAggro && Body.TargetObject != null)
        {
            if (!Message)
            {
                BroadcastMessage(String.Format("The {0} shouts, 'The power of Mithra cleanses this holy place. Out! Out! I command you!\n" +
                                               "The {1} shouts, 'Come to me, my servants! Come and serve in the glory of Mithra!", Body.Name,Body.Name));
                Message = true;
            }
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "AndaniaBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}