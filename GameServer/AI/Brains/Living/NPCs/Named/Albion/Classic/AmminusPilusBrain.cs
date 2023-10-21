using System;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class AmminusPilusBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public AmminusPilusBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    private bool SpawnAdds = false;
    private bool RemoveAdds = false;
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
        }
    }
    public override void Think()
    {
        if(!CheckProximityAggro())
        {
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.IsAlive && npc != null && npc.Brain is PilusAddBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
            SpawnAdds = false;
        }
        if(HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (!SpawnAdds)
            {
                BroadcastMessage("The Amminus pilus says, \"I require assistance!\"");
                SpawnPilusAdds();
                SpawnAdds = true;
            }
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is PilusAddBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && target.IsAlive && target != null)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
    private void SpawnPilusAdds()
    {
        for (int i = 0; i < 4; i++)
        {
            PilusAdd npc = new PilusAdd();
            npc.X = Body.X + Util.Random(-100, 100);
            npc.Y = Body.Y + Util.Random(-100, 100);
            npc.Z = Body.Z;
            npc.Heading = Body.Heading;
            npc.CurrentRegion = Body.CurrentRegion;
            npc.AddToWorld();
        }
    }
}

#region Pilus adds
public class PilusAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public PilusAddBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Pilus adds