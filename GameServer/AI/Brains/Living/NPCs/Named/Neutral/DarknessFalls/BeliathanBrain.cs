using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

#region Initializator Brain
public class BeliathanInitBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BeliathanInitBrain()
        : base()
    {
    }

    public override int ThinkInterval => 600000; // 10 min

    public override void Think()
    {
        var princeStatus = WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID);
        var princeCount = 0;
        var beliathan = WorldMgr.GetNPCsByNameFromRegion("Beliathan", 249, (ERealm) 0);
        bool beliSpawned;

        if (beliathan.Length == 0)
        {
            beliSpawned = false;
        }
        else
        {
            beliSpawned = true;
        }

        if (!beliSpawned)
        {
            foreach (GameNpc npc in princeStatus)
            {
                if (!npc.Name.ToLower().Contains("prince")) continue;
                princeCount++;
            }

            if (princeCount == 0)
            {
                SpawnBeliathan();
            }
        }

        base.Think();
    }

    public void SpawnBeliathan()
    {
        BroadcastMessage("The tunnels rumble and shake..");
        Beliathan Add = new Beliathan();
        Add.X = Body.X;
        Add.Y = Body.Y;
        Add.Z = Body.Z;
        Add.CurrentRegion = Body.CurrentRegion;
        Add.Heading = 4072;
        Add.AddToWorld();
    }

    public void BroadcastMessage(string message)
    {
        foreach (GamePlayer player in ClientService.GetPlayersOfRegion(Body.CurrentRegion))
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
    }
}
#endregion Initializator Brain

#region Beliathan
public class BeliathanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is BeliathanMinionBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
            RemoveAdds = false;
        base.Think();
    }
}
#endregion Beliathan

public class BeliathanMinionBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BeliathanMinionBrain()
    {
        AggroLevel = 100;
        AggroRange = 450;
    }
}