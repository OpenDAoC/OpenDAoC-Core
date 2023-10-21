using System;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

public class BlightBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BlightBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 2000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(3500))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }
    public static bool canGrowth = true;
    bool SpamMessage = false;
    public override void Think()
    {
        if(Body.IsAlive && canGrowth && Body.Size < 200)
        {
            Body.Size += 5;
        }

        if(Body.Size >= 200)
            canGrowth = false;

        if (!canGrowth && !SpamMessage)
        {
            BroadcastMessage("Blight has taken it's true form! It turns its deadful stare upon you!");
            SpamMessage = true;
        }

        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }
}

#region Fire Blight
public class FireBlightBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FireBlightBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc != Body && npc.Brain is FireBlightBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && brain != Body.Brain && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
}
#endregion Fire Blight

#region Late Blight
public class LateBlightBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LateBlightBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc != Body && npc.Brain is LateBlightBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && brain != Body.Brain && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
}
#endregion Late Blight

#region Flesh Blight
public class FleshBlightBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FleshBlightBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc != Body && npc.Brain is FleshBlightBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && brain != Body.Brain && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
}
#endregion Flesh Blight

#region Blight Controller
public class BlightControllerBrain : APlayerVicinityBrain
{
	private static readonly log4net.ILog log =
		log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public BlightControllerBrain()
		: base()
	{
		ThinkInterval = 1000;
	}
	public static bool CreateLateBlight = false;
	public static bool CreateFleshBlight = false;
	public static bool CreateBlight = false;

	public override void Think()
	{
		if(FireBlight.FireBlightCount == 8)
			SpawnLateBlight();
		if (LateBlight.LateBlightCount == 4)
			SpawnFleshBlight();
		if (FleshBlight.FleshBlightCount == 2)
			SpawnBlight();
	}

	public override void KillFSM()
	{
		
	}

	public void SpawnLateBlight()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc.Brain is LateBlightBrain)
				return;
		}
		if (FireBlight.FireBlightCount == 8 && !CreateLateBlight)
		{
			for (int i = 0; i < 4; i++)
			{
				LateBlight boss = new LateBlight();
				boss.X = Body.X + Util.Random(-500, 500);
				boss.Y = Body.Y + Util.Random(-500, 500);
				boss.Z = Body.Z;
				boss.Heading = Body.Heading;
				boss.CurrentRegion = Body.CurrentRegion;
				boss.AddToWorld();
			}
			CreateLateBlight = true;
		}
	}
	public void SpawnFleshBlight()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc.Brain is FleshBlightBrain)
				return;
		}
		if (LateBlight.LateBlightCount == 4 && FireBlight.FireBlightCount == 8 && !CreateFleshBlight)
		{
			for (int i = 0; i < 2; i++)
			{
				FleshBlight boss = new FleshBlight();
				boss.X = Body.X + Util.Random(-500, 500);
				boss.Y = Body.Y + Util.Random(-500, 500);
				boss.Z = Body.Z;
				boss.Heading = Body.Heading;
				boss.CurrentRegion = Body.CurrentRegion;
				boss.AddToWorld();
			}
			CreateFleshBlight = true;
		}
	}
	public void SpawnBlight()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc.Brain is BlightBrain)
				return;
		}
		if (FleshBlight.FleshBlightCount == 2 && LateBlight.LateBlightCount == 4 && FireBlight.FireBlightCount == 8 && !CreateBlight)
		{
			Blight boss = new Blight();
			boss.X = Body.X + Util.Random(-500, 500);
			boss.Y = Body.Y + Util.Random(-500, 500);
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.AddToWorld();
			CreateBlight = true;
		}
	}
}
#endregion Blight Controller