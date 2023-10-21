using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

#region Sys'sro the Ruthless
public class SyssroRuthlessBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SyssroRuthlessBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	public static bool IsTargetPicked = false;
	public static bool IsPulled = false;
	List<GamePlayer> Port_Enemys = new List<GamePlayer>();
	public int ThrowPlayer(EcsGameTimer timer)
	{
		if (Body.IsAlive)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Port_Enemys.Contains(player))
						{
							if (player != Body.TargetObject)
							{
								Port_Enemys.Add(player);
							}
						}
					}
				}
			}
			if (Port_Enemys.Count > 0)
			{
				GamePlayer Target = (GamePlayer)Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
				RandomTarget = Target;
				if (RandomTarget.IsAlive && RandomTarget != null)
				{
					RandomTarget.MoveTo(50, 41489, 40699, 8145, 2096);
					Port_Enemys.Remove(RandomTarget);
					RandomTarget = null;//reset random target to null
					IsTargetPicked = false;
				}
			}
		}
		return 0;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsTargetPicked = false;
			IsPulled = false;
			RandomTarget = null;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			if (IsTargetPicked == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(10000, 15000));//timer to port and pick player
				IsTargetPicked = true;
            }
			if (IsPulled == false)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "SyssroBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
						}
					}
				}
				IsPulled = true;
			}
		}
		base.Think();
	}
}
#endregion Sys'sro the Ruthless

#region Pit Monster add
public class PitMonsterBrain : StandardMobBrain
{
	public PitMonsterBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 0;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		foreach(GamePlayer player in Body.GetPlayersInRadius((ushort)Body.AttackRange))
		{
			if(player != null)
			{
				if(player.IsAlive && player.Client.Account.PrivLevel == 1 && !AggroTable.ContainsKey(player))
				{
					AddToAggroList(player, 200);
				}
			}
			if(player == null || !player.IsAlive)
			{
				ClearAggroList();
			}
		}
		base.Think();
	}
}
#endregion Pit Monster add