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
		private bool hasGrown = false;

		/// <summary>
		/// Put on lower think cycle so mobs spawn a little slower.
		/// </summary>
		public AlluvianGlobuleBrain()
			: base()
		{
			ThinkInterval = 3000;
			hasGrown = false;
		}

		/// <summary>
		/// Determine if there's currently a storm to do effect.
		/// Special logic for group fights.
		/// This mob also casts a DD. Will leave out until gameloop is ready.
		/// </summary>
		public override void Think()
		{
			if (CheckStorm())
			{
				if (!hasGrown)
				{
					Grow();
				}
			}
			if (!Body.IsReturningHome)
			{
				if (!Body.AttackState && AggroRange > 0)
				{
					var currentPlayersSeen = new List<GamePlayer>();
					foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange, true))
					{
						if (!PlayersSeen.Contains(player))
						{
							PlayersSeen.Add(player);
						}
						currentPlayersSeen.Add(player);
					}
					for (int i = 0; i < PlayersSeen.Count; i++)
					{
						if (!currentPlayersSeen.Contains(PlayersSeen[i]))
						{
							PlayersSeen.RemoveAt(i);
						}
					}
				}
				if (!Body.AttackState && AggroLevel > 0)
				{
					CheckPlayerAggro();
					CheckNPCAggro();
				}
				if (HasAggro)
				{
					AttackMostWanted();
					return;
				}
				if (!HasAggro)
				{
					if (Body.AttackState)
					{
						Body.StopAttack();
					}

					Body.TargetObject = null;
				}
			}
			if (!Body.AttackState && !Body.IsMoving && !Body.InCombat)
			{
				// loc range around the lake that Alluvian spanws.
				Body.WalkTo(544196 + Util.Random(1, 3919), 514980 + Util.Random(1, 3200), 3140 + Util.Random(1, 540), 80);
			}
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