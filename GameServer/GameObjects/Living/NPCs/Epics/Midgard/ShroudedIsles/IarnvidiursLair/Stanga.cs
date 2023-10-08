﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Stanga : GameEpicBoss
	{
		public Stanga() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Stanga Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20; // dmg reduction for melee dmg
				case EDamageType.Crush: return 20; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override double AttackDamage(DbInventoryItem weapon)
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
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83014);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(159);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(159));

			StangaBrain sbrain = new StangaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class StangaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public StangaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
			}
			if (Body.InCombat && HasAggro && Body.TargetObject != null)
			{
				if (IsPulled == false)
				{
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.PackageID == "StangaBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if (Body.TargetObject != null)
				{
					if (Util.Chance(15))
					{
						if (LivingHasEffect(Body.TargetObject as GameLiving, Stanga_SC_Debuff) == false && Body.TargetObject.IsVisibleTo(Body))
						{
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastSCDebuff), 1000);
						}
					}
					if (Util.Chance(15))
					{
						if (LivingHasEffect(Body.TargetObject as GameLiving, StangaDisease) == false && Body.TargetObject.IsVisibleTo(Body))
						{
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDisease), 1000);
						}
					}
				}
			}
			base.Think();
		}
		public int CastSCDebuff(ECSGameTimer timer)
		{
			if (Body.TargetObject != null && HasAggro && Body.IsAlive)
			{
				Body.CastSpell(Stanga_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		public int CastDisease(ECSGameTimer timer)
		{
			if (Body.TargetObject != null && HasAggro && Body.IsAlive)
			{
				Body.CastSpell(StangaDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		private Spell m_StangaDisease;
		private Spell StangaDisease
		{
			get
			{
				if (m_StangaDisease == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4375;
					spell.Icon = 4375;
					spell.Name = "Black Plague";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 4375;
					spell.Radius = 450;
					spell.Range = 0;
					spell.Duration = 186;
					spell.SpellID = 11819;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
					m_StangaDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_StangaDisease);
				}
				return m_StangaDisease;
			}
		}
		private Spell m_Stanga_SC_Debuff;
		private Spell Stanga_SC_Debuff
		{
			get
			{
				if (m_Stanga_SC_Debuff == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 60;
					spell.ClientEffect = 2767;
					spell.Icon = 2767;
					spell.Name = "Stanga's Debuff S/C";
					spell.TooltipId = 2767;
					spell.Range = 1500;
					spell.Value = 80;
					spell.Radius = 400;
					spell.SpellID = 11818;
					spell.Target = "Enemy";
					spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Stanga_SC_Debuff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Stanga_SC_Debuff);
				}
				return m_Stanga_SC_Debuff;
			}
		}
	}
}
