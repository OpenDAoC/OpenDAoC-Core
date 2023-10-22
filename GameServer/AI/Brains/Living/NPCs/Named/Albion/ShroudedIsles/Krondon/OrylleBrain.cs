using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class OrylleBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public OrylleBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
		CanBAF = false;
	}
	public static bool IsPulled = false;
    #region Throw Player
    List<GamePlayer> Port_Enemys = new List<GamePlayer>();
	public static bool IsTargetPicked = false;
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
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
							//if (player != Body.TargetObject)
								Port_Enemys.Add(player);
						}
					}
				}
			}
			if (Port_Enemys.Count > 0)
			{
				GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
				RandomTarget = Target;
				if (RandomTarget.IsAlive && RandomTarget != null)
				{
					RandomTarget.MoveTo(61, 31406, 69599, 15605, 2150);
					Port_Enemys.Remove(RandomTarget);
				}
			}
			RandomTarget = null;//reset random target to null
			IsTargetPicked = false;
		}
		return 0;
	}
    #endregion
    public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			RandomTarget = null;//throw
			IsTargetPicked = false;//throw
		}
		if (HasAggro && Body.TargetObject != null)
		{
			if (IsTargetPicked == false && OrshomFire.FireCount > 0)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(15000, 20000));//timer to port and pick player
				IsTargetPicked = true;
			}
			if(IsPulled==false)
            {
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is OrshomBrain brain)
						{
							if (!brain.HasAggro)
								AddAggroListTo(brain);
							IsPulled = true;
						}
					}
				}
			}
		}
		base.Think();
	}
}