using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using System.Collections.Generic;

namespace DOL.GS
{
	public class MorgusUrgalorg : GameEpicBoss
	{
		public MorgusUrgalorg() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Morgus Urgalorg Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 60000; }
		}
		public override void StartAttack(GameObject target)
        {
        }
        public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(72, 75));
			Name = "Morgus Urgalorg";
			Size = 120;

			Strength = 280;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			MorgusUrgalorgBrain sbrain = new MorgusUrgalorgBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MorgusUrgalorgBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MorgusUrgalorgBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public static bool IsPulled = false;
		public static bool CanCast = false;
		List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
		public void PickRandomTarget()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DD.Contains(player) && player != Body.TargetObject)
							Enemys_To_DD.Add(player);
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetTarget), 4500);
					CanCast = true;
				}
			}
		}
		public int ResetTarget(ECSGameTimer timer)//reset here so boss can start dot again
		{
			RandomTarget = null;
			CanCast = false;
			return 0;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				RandomTarget = null;
				CanCast = false;
				IsPulled = false;
				if (Enemys_To_DD.Count > 0)
					Enemys_To_DD.Clear();
			}
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Morgus_DD))
					Body.Spells.Add(Morgus_DD);
				if (!Body.Spells.Contains(Morgus_Bolt))
					Body.Spells.Add(Morgus_Bolt);
				if (!Body.Spells.Contains(Morgus_Bolt2))
					Body.Spells.Add(Morgus_Bolt2);
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (IsPulled == false)
					{
						foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
						{
							if (npc != null)
							{
								if (npc.IsAlive && npc.PackageID == "MorgusBaf")
									AddAggroListTo(npc.Brain as StandardMobBrain);								
							}
						}
						IsPulled = true;
					}
					if (!Body.IsCasting && !Body.IsMoving)
					{
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, spells.Range - 50, 5000);
							
								if (Util.Chance(100))
								{
									PickRandomTarget();
									if (CanCast)
									{
										GameLiving oldTarget = Body.TargetObject as GameLiving;
										if (RandomTarget != null && RandomTarget.IsAlive && CanCast)
										{
											if (!Body.IsCasting && Body.GetSkillDisabledDuration(Morgus_Bolt) == 0)
											{
												Body.TargetObject = RandomTarget;
												Body.TurnTo(RandomTarget);
												Body.CastSpell(Morgus_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
											}
											if (!Body.IsCasting && Body.GetSkillDisabledDuration(Morgus_Bolt) > 0  && Body.GetSkillDisabledDuration(Morgus_Bolt2) == 0)
											{
												Body.TargetObject = RandomTarget;
												Body.TurnTo(RandomTarget);
												Body.CastSpell(Morgus_Bolt2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
											}
										}
										if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
									}
									else
									{
										Body.TurnTo(Body.TargetObject);
										Body.CastSpell(Morgus_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
							}
						}
					}
				}
			}
			base.Think();
		}
		private Spell m_Morgus_DD;
		private Spell Morgus_DD
		{
			get
			{
				if (m_Morgus_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 368;
					spell.Icon = 368;
					spell.TooltipId = 368;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Ebullient Blast";
					spell.Range = 1500;
					spell.Radius = 250;
					spell.SpellID = 11892;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_Morgus_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Morgus_DD);
				}
				return m_Morgus_DD;
			}
		}
		private Spell m_Morgus_Bolt;
		private Spell Morgus_Bolt
		{
			get
			{
				if (m_Morgus_Bolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = 20;
					spell.ClientEffect = 69;
					spell.Icon = 69;
					spell.TooltipId = 69;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Lava's Fury";
					spell.Range = 1800;
					spell.SpellID = 11893;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Bolt.ToString();
					spell.Uninterruptible = true;
					m_Morgus_Bolt = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Morgus_Bolt);
				}
				return m_Morgus_Bolt;
			}
		}
		private Spell m_Morgus_Bolt2;
		private Spell Morgus_Bolt2
		{
			get
			{
				if (m_Morgus_Bolt2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = 20;
					spell.ClientEffect = 318;
					spell.Icon = 318;
					spell.TooltipId = 318;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Lava's Fury";
					spell.Range = 1800;
					spell.SpellID = 11894;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Bolt.ToString();
					spell.Uninterruptible = true;
					m_Morgus_Bolt2 = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Morgus_Bolt2);
				}
				return m_Morgus_Bolt2;
			}
		}
	}
}
