﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;

namespace DOL.GS
{
	public class Lydsyg : GameEpicNPC
	{
		public Lydsyg() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Lydsyg Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(DbInventoryItems weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(45))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(LydsygDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
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
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
		public override int MaxHealth
		{
			get { return 10000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83009);
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

			LydsygBrain sbrain = new LydsygBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private Spell m_LydsygDisease;
		private Spell LydsygDisease
		{
			get
			{
				if (m_LydsygDisease == null)
				{
					DbSpells spell = new DbSpells();
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
					spell.Range = 450;
					spell.Duration = 3000;//50min
					spell.SpellID = 11822;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_LydsygDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LydsygDisease);
				}
				return m_LydsygDisease;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class LydsygBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LydsygBrain() : base()
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
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
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
							if (npc.IsAlive && npc.PackageID == "LydsygBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if (Body.TargetObject != null)
				{
					Body.CastSpell(Boss_Lifedrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}


		private Spell m_Boss_Lifedrain;
		private Spell Boss_Lifedrain
		{
			get
			{
				if (m_Boss_Lifedrain == null)
				{
					DbSpells spell = new DbSpells();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = Util.Random(10, 15);
					spell.ClientEffect = 2610;
					spell.Icon = 2610;
					spell.Name = "Drain Life";
					spell.TooltipId = 2610;
					spell.Damage = 250;
					spell.Range = 1500;
					spell.Value = -30;
					spell.LifeDrainReturn = 30;
					spell.SpellID = 11821;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Lifedrain.ToString();
					spell.DamageType = (int)eDamageType.Body;
					spell.Uninterruptible = true;
					m_Boss_Lifedrain = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Lifedrain);
				}
				return m_Boss_Lifedrain;
			}
		}
	}
}


