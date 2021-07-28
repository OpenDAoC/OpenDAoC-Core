using System;
using System.Collections.Generic;
using DOL.Events;
using DOL.GS;
using DOL.GS.Scheduler;


namespace DOL.AI.Brain
{
    public class AlluvianMob : GameNPC
	{
		/// <summary>
		/// Blank for spawned mobs, which dont REspawn!
		/// </summary>
		public override void StartRespawn()
		{
		}

		public override void SaveIntoDatabase()
		{
		}
	}

	public class Alluvian : GameNPC
	{
		public Alluvian() : base()
		{
			SetOwnBrain(new AlluBrain());
		}

		private static int m_globuleCount;

		//Holds the current number of globules, be sure to GlobuleNumber--; when a globule dies.
		public static int GlobuleNumber
		{
			get { return m_globuleCount; }
			set { m_globuleCount = value; }
		}

		public int SpawnGlobule()
		{
			AlluvianMob globulespawn = new AlluvianMob();
			globulespawn.Model = 928;
			globulespawn.Size = 40;
			globulespawn.Level = (byte)Util.Random(3, 4);
			globulespawn.Name = "alluvian globule";
			globulespawn.CurrentRegionID = 51;
			globulespawn.Heading = Heading;
			globulespawn.Realm = 0;
			globulespawn.CurrentSpeed = 0;
			globulespawn.MaxSpeedBase = 191;
			globulespawn.GuildName = "";
			globulespawn.X = X;
			globulespawn.Y = Y;
			globulespawn.Z = 3083;
			globulespawn.RespawnInterval = -1;
			globulespawn.BodyType = 4;
			globulespawn.Flags ^= eFlags.FLYING;

			GlobuleBrain brain = new GlobuleBrain();
			brain.AggroLevel = 70;
			brain.AggroRange = 500;
			globulespawn.SetOwnBrain(brain);
			globulespawn.AutoSetStats();
			globulespawn.AddToWorld();
			GlobuleNumber++;
			brain.WalkFromSpawn();
			//Tell me when you die so I can GlobuleNumber--;
			GameEventMgr.AddHandler(globulespawn, GameNPCEvent.Dying, new DOLEventHandler(GlobuleHasDied));

			return 0;
		}

		public static void GlobuleHasDied(DOLEvent e, object sender, EventArgs args)
		{
			GlobuleNumber--;
			//Remove the handler so they don't pile up.
			GameEventMgr.RemoveHandler(sender, GameNPCEvent.Dying, new DOLEventHandler(GlobuleHasDied));
			return;
		}
	}
}

namespace DOL.AI.Brain
{
    public class GlobuleBrain : StandardMobBrain
	{
		private bool hasGrown = false;
		public GlobuleBrain()
			: base()
		{
			ThinkInterval = 3000;
			hasGrown = false;

		}

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
							//Body.FireAmbientSentence(GameNPC.eAmbientTrigger.seeing, player as GameLiving);
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


				//if (!Body.AttackState && (AggroLevel > 0 || (Properties.USE_NPC_FACTIONS)))
				if (!Body.AttackState && AggroLevel > 0)
				{
					//if (!IsHostile)
					//{
					//	CheckPlayerAggro();
					//}
					CheckPlayerAggro();
					CheckNPCAggro();
				}

				//Maybe
				//if (Body.AttackState)
				//{
				//	long delay = 18000 * (1 + ((100 - Body.HealthPercent) / 250));
				//	long lastattack = Body.TempProperties.getProperty<long>(GameLiving.LAST_ATTACK_TIME, 0);

				//	if ((LastNaturalAggro == 0 || LastNaturalAggro + delay < Body.CurrentRegion.Time)
				//		&& (Body.LastAttackedByEnemyTick == 0 || Body.LastAttackedByEnemyTick + delay < Body.CurrentRegion.Time)
				//		&& (lastattack == 0 || lastattack + delay < Body.CurrentRegion.Time))
				//	{
				//		Body.StopAttack();

				//		Body.WalkToSpawn();
				//		return;
				//	}
				//}

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
				Body.WalkTo(544196 + Util.Random(1, 3919), 514980 + Util.Random(1, 3200), 3140 + Util.Random(1, 540), 80); // loc range around the lake that Alluvian spanws.
			}
		}

		protected override void AttackMostWanted()
		{
			if (!IsActive)
			{
				return;
			}

			Body.TargetObject = CalculateNextAttackTarget();

			if (Body.TargetObject != null)
			{
				if (!CheckSpellCast())
				{
					Body.StartAttack(Body.TargetObject);
				}
			}
		}

		private bool CheckSpellCast()
		{
			if (Body.IsCasting || Body.IsBeingInterrupted)
			{
				return false;
			}

			SpellLine mobspells = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
			if (mobspells == null)
			{
				return false;
			}

			Spell nuke = SkillBase.FindSpell(1604, mobspells);
			Body.CastSpell(nuke, mobspells);
			return true;
		}

		public void WalkFromSpawn()
		{
			const int roamingRadius = 500;
			double targetX = Body.SpawnPoint.X + Util.Random(-roamingRadius, roamingRadius);
			double targetY = Body.SpawnPoint.Y + Util.Random(-roamingRadius, roamingRadius);
			Body.WalkTo((int)targetX, (int)targetY, 3083, 150);
		}

		public bool CheckStorm()
		{
			var currentStorm = GameServer.Instance.WorldManager.WeatherManager[Body.CurrentRegionID];
			var Glob = Body as AlluvianMob;
			if (currentStorm != null)
			{
				var weatherCurrentPosition = currentStorm.CurrentPosition(SimpleScheduler.Ticks);
				//var currentStorm = WeatherMgr.GetWeatherForRegion(Body.CurrentRegionID);
				if (Body.X > (weatherCurrentPosition - currentStorm.Width) && Body.X < weatherCurrentPosition)
				{
					//var playersInRange = Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);
					//1 in 5 chance? sure, I dunno, this can be tweaked
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
		public void Grow()
		{
			//It's raining, now the globules can grow
			Body.Size = 95;
			Body.Level = (byte)Util.Random(10, 11);
			Body.AutoSetStats();
			hasGrown = true;
		}
	}

	public class AlluBrain : StandardMobBrain
	{
		public override void Think()
		{
			Alluvian mob = Body as Alluvian;

			if (Alluvian.GlobuleNumber < 12)
			{
				mob.SpawnGlobule();
			}
			base.Think();

		}
	}
}