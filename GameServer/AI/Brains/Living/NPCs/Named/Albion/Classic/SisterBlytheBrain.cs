using System;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Sister Blythe
public class SisterBlytheBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SisterBlytheBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	public static int FallenExecutionerCount = 0;
	private bool Message1 = false;
	private bool Message2 = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			Message1 = false;
			Message2 = false;
		}
		if(HasAggro && Body.TargetObject != null)
        {
			GameLiving target = Body.TargetObject as GameLiving;
			if (!Message1)
            {
				switch(Util.Random(1,2))
                {
					case 1: BroadcastMessage("Sister Blythe shouts in a language you cannot understand!"); break;
					case 2: BroadcastMessage(String.Format("{0} says, \"Come my pets! Let us show these fools what comes of failure!\"", Body.Name)); break;
				}
				if(FallenExecutionerCount > 0)
					BroadcastMessage("The fallen executioner says, \"By your command!\"");
				Message1 = true;
            }
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is FallenExecutionerBrain brain)
				{
					if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
						brain.AddToAggroList(target, 10);
				}
			}
			if(FallenExecutionerCount < 4)
            {
				if (!Message2)
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SisterSummonEffect), Util.Random(6000,10000));
					Message2 = true;
				}
			}
		}
		base.Think();
	}
	private int SisterSummonEffect(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(Body, Body, 6040, 0, false, 0x01);
			}
			BroadcastMessage("Sister Blythe says, \"Witness the power of Lord Arawn!\"");
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMoreExecutioners), 3000);
		}
		return 0;
	}
	private int SpawnMoreExecutioners(EcsGameTimer timer)
    {
		if (Body.IsAlive && HasAggro)
		{
			SpawnExecutioners();
			Message2 = false;
		}
		return 0;
    }
	private void SpawnExecutioners()
	{
		Point3D spawn = new Point3D(322192, 671493, 2764);
		for (int i = 0; i < 4; i++)
		{
			if (FallenExecutionerCount < 4)
			{
				FallenExecutioner npc = new FallenExecutioner();
				npc.X = spawn.X + Util.Random(-150, 150);
				npc.Y = spawn.Y + Util.Random(-150, 150);
				npc.Z = spawn.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
#endregion Sister Blythe

#region Fallen Executioners
public class FallenExecutionerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public FallenExecutionerBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Fallen Executioners