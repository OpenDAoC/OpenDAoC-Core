using System.Collections.Generic;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

public class GlacierGiantBrain : EpicBossBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public GlacierGiantBrain() : base()
	{
		AggroLevel = 0;//is neutral
		AggroRange = 600;
		ThinkInterval = 1500;
	}	
	
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			Clear_List = false;
			RandomTarget = null;
			if (Teleported_Players.Count>0)
				Teleported_Players.Clear();
			if(Enemys_To_Port.Count>0)
				Enemys_To_Port.Clear();
		}
		if (Body.InCombat && Body.IsAlive && HasAggro)
		{
			if (Body.TargetObject != null)
			{
				if(Util.Chance(20) && Body.HealthPercent > 15)//dont port players if it's low on health
					TeleportPlayer();
			}
		}
		if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			Body.Health = Body.MaxHealth;
		base.Think();
	}

	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	List<GamePlayer> Enemys_To_Port = new List<GamePlayer>();
	List<GamePlayer> Teleported_Players = new List<GamePlayer>();
	public void TeleportPlayer()
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
		{
			if (player != null)
			{
				if (player.IsAlive && player.Client.Account.PrivLevel == 1 && player != Body.TargetObject)
				{
					if (!Enemys_To_Port.Contains(player) && !Teleported_Players.Contains(RandomTarget))
						Enemys_To_Port.Add(player);
				}
			}
		}
		if (Enemys_To_Port.Count == 0)
			return;
		else
		{
			GamePlayer PortTarget = (GamePlayer)Enemys_To_Port[Util.Random(0, Enemys_To_Port.Count - 1)];
			RandomTarget = PortTarget;
			if (RandomTarget.IsAlive && RandomTarget != null && RandomTarget.IsWithinRadius(Body,2000) && !Teleported_Players.Contains(RandomTarget))
			{
				switch(Util.Random(1,6))
                {
					case 1: RandomTarget.MoveTo(Body.CurrentRegionID, 663537 + Util.Random(-2000,2000), 626415 + Util.Random(-2000, 2000), 7790 + Util.Random(300, 600), Body.Heading); break;
					case 2: RandomTarget.MoveTo(Body.CurrentRegionID, 647342 + Util.Random(-2000, 2000), 617589 + Util.Random(-2000, 2000), 8533 + Util.Random(300, 600), Body.Heading); break;
					case 3: RandomTarget.MoveTo(Body.CurrentRegionID, 645157 + Util.Random(-2000, 2000), 630671 + Util.Random(-2000, 2000), 11530 + Util.Random(300, 600), Body.Heading); break;
					case 4: RandomTarget.MoveTo(Body.CurrentRegionID, 654502 + Util.Random(-2000, 2000), 630523 + Util.Random(-2000, 2000), 8762 + Util.Random(300, 600), Body.Heading); break;
					case 5: RandomTarget.MoveTo(Body.CurrentRegionID, 670626 + Util.Random(-2000, 2000), 630046 + Util.Random(-2000, 2000), 7515 + Util.Random(300, 600), Body.Heading); break;
					case 6: RandomTarget.MoveTo(Body.CurrentRegionID, 642185 + Util.Random(-2000, 2000), 620183 + Util.Random(-2000, 2000), 10014 + Util.Random(300, 600), Body.Heading); break;
				}
				Enemys_To_Port.Remove(RandomTarget);
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player != null)
						player.Out.SendMessage("Glacier Giant kick away " + RandomTarget.Name + "!", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
				}					
				if (RandomTarget != null && RandomTarget.IsAlive && !Teleported_Players.Contains(RandomTarget))
				{
					Teleported_Players.Add(RandomTarget);
					if (Clear_List == false)
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ListCleanTimer), 45000);//clear list of teleported players, so it will not pick instantly already teleported target
						Clear_List = true;
					}
				}					
				RandomTarget = null;
			}
		}
	}
	public static bool Clear_List = false;
	public int ListCleanTimer(EcsGameTimer timer)
    {
		if (Body.IsAlive && Body.InCombat && HasAggro && Teleported_Players.Count > 0)
		{
			Teleported_Players.Clear();
			Clear_List = false;
		}
		return 0;
    }
}