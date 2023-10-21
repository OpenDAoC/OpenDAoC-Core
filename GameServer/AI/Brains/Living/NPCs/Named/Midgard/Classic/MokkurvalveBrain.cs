using System;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class MokkurvalveBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public MokkurvalveBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }
    private bool CanSpawnShard = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            CanSpawnShard = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is MokkurvalveAddsBrain)
                        npc.Die(Body);
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if(!CanSpawnShard)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnShards), Util.Random(15000, 35000));
                CanSpawnShard = true;
            }
        }
        base.Think();
    }
    private int SpawnShards(EcsGameTimer timer)
    {
        if (HasAggro && Body.TargetObject != null)
        {
            BroadcastMessage("Part of " + Body.Name + "'s body falls to the ground.");
            MokkurvalveAdds add = new MokkurvalveAdds();
            add.X = Body.X + Util.Random(-200, 200);
            add.Y = Body.Y + Util.Random(-200, 200);
            add.Z = Body.Z;
            add.Heading = Body.Heading;
            add.CurrentRegion = Body.CurrentRegion;
            add.AddToWorld();
        }
        CanSpawnShard = false;
        return 0;
    }
}

#region Mokkurvalve adds
public class MokkurvalveAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public MokkurvalveAddsBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Mokkurvalve adds