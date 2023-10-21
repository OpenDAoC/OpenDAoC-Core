using System;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class DaewainBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public DaewainBrain() : base()
    {
        AggroLevel = 0;
        AggroRange = 400;
        ThinkInterval = 1000;
    }
    ushort oldModel;
    ENpcFlags oldFlags;
    bool changed;
    bool playerOnBridge = false;
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Think()
    {
        if(Body.IsAlive)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(800))
            {
                if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                    playerOnBridge = true;
            }
            if (playerOnBridge)
            {
                if (changed)
                {
                    Body.Flags = oldFlags;
                    Body.Model = oldModel;
                    BroadcastMessage("Daewain croaks softly as he rests in the shade under the bridge.");
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
        }
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "DaewainBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}