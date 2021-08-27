using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Scheduler;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Brains for Alluvian mob in Albion SI Avalon Isle
	/// </summary>
    public class AlluvianGlobuleBrain : StandardMobBrain
	{
		public bool hasGrown = false;

		/// <summary>
		/// Put on lower think cycle so mobs spawn a little slower.
		/// </summary>
		public AlluvianGlobuleBrain()
			: base()
		{
			ThinkInterval = 3000;
			hasGrown = false;

			FSM.ClearStates();
			FSM.Add(new AlluvianGlobuleState_IDLE(FSM, this));
			FSM.Add(new AlluvianGlobuleState_ROAMING(FSM, this));
			FSM.Add(new StandardMobState_WAKING_UP(FSM, this));
			FSM.Add(new StandardMobState_AGGRO(FSM, this));
			FSM.Add(new StandardMobState_RETURN_TO_SPAWN(FSM, this));
			FSM.Add(new StandardMobState_PATROLLING(FSM, this));
			FSM.Add(new StandardMobState_DEAD(FSM, this));

			FSM.SetCurrentState(eFSMStateType.WAKING_UP);
		}

		/// <summary>
		/// Determine most wanted player.
		/// </summary>
		public override void AttackMostWanted()
		{
			if (!IsActive)
			{
				return;
			}
			Body.TargetObject = CalculateNextAttackTarget();
		}

		/// <summary>
		/// Walk from spawn point
		/// </summary>
		public void WalkFromSpawn()
		{
			const int roamingRadius = 500;
			double targetX = Body.SpawnPoint.X + Util.Random(-roamingRadius, roamingRadius);
			double targetY = Body.SpawnPoint.Y + Util.Random(-roamingRadius, roamingRadius);
			Body.WalkTo((int)targetX, (int)targetY, 3083, 150);
		}

		/// <summary>
		/// Check if currently in the storm, send out special effect to all players.
		/// </summary>
		/// <returns></returns>
		public bool CheckStorm()
		{
			var currentStorm = GameServer.Instance.WorldManager.WeatherManager[Body.CurrentRegionID];
			var Glob = Body as Alluvian;
			if (currentStorm != null)
			{
				var weatherCurrentPosition = currentStorm.CurrentPosition(SimpleScheduler.Ticks);
				if (Body.X > (weatherCurrentPosition - currentStorm.Width) && Body.X < weatherCurrentPosition)
				{
					if (Util.Random(4) == 0)
					{
						foreach (GamePlayer player in Glob.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							player.Out.SendSpellEffectAnimation(Glob, Glob, (ushort)6053, 0, false, 1);
						}
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Grow in size and level
		/// </summary>
		public void Grow()
		{
			Body.Size = 95;
			Body.Level = (byte)Util.Random(10, 11);
			Body.AutoSetStats();
			hasGrown = true;
		}
	}
}