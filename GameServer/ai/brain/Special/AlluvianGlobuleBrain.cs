using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scheduler;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Brains for Alluvian mob in Albion SI Avalon Isle
	/// </summary>
	public class AlluvianGlobuleBrain : StandardMobBrain
	{
		internal bool hasGrown = false;

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

			base.Think();
		}

		/// <summary>
		/// Walk from spawn point
		/// </summary>
		public void WalkFromSpawn()
		{
			const int roamingRadius = 500;
			double targetX = Body.SpawnPoint.X + Util.Random(-roamingRadius, roamingRadius);
			double targetY = Body.SpawnPoint.Y + Util.Random(-roamingRadius, roamingRadius);
			Body.WalkTo(new Point3D((int) targetX, (int) targetY, 3083), 150);
		}

		/// <summary>
		/// Check if currently in the storm, send out special effect to all players.
		/// </summary>
		/// <returns></returns>
		public bool CheckStorm()
		{
			var currentStorm = GameServer.Instance.WorldManager.WeatherManager[Body.CurrentRegionID];
			if (currentStorm != null)
			{
				var weatherCurrentPosition = currentStorm.CurrentPosition(SimpleScheduler.Ticks);
				if (Body.X > (weatherCurrentPosition - currentStorm.Width) && Body.X < weatherCurrentPosition)
				{
					if (Util.Random(4) == 0)
					{
						foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							player.Out.SendSpellEffectAnimation(Body, Body, (ushort)6053, 0, false, 1);
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
			Body.SetStats();
			hasGrown = true;
			Message.MessageToArea(Body, "Storm-water sluices into the alluvian globule, and it swells to twice its size!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
		}
	}
}
