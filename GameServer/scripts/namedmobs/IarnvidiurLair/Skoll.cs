using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Events;

namespace DOL.GS
{
	public class Skoll : GameEpicBoss
	{
		public Skoll() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Skoll Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40; // dmg reduction for melee dmg
				case eDamageType.Crush: return 40; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
			}
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
			get { return 30000; }
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(35))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(SkollDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
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
			if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83027);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(159);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(159));

			SkollBrain sbrain = new SkollBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private Spell m_SkollDD;
		private Spell SkollDD
		{
			get
			{
				if (m_SkollDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2919;
					spell.Icon = 2919;
					spell.Name = "Skoll Bite";
					spell.TooltipId = 2919;
					spell.Damage = 250;
					spell.Range = 350;
					spell.Value = 10;
					spell.Duration = 20;
					spell.SpellID = 11810;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageWithDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_SkollDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SkollDD);
				}
				return m_SkollDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class SkollBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SkollBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public override void Think()
		{
			if (!HasAggressionTable())
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
							if (npc.IsAlive && npc.PackageID == "SkollBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if(Body.TargetObject != null)
                {
					GameLiving target = Body.TargetObject as GameLiving;
					if (Util.Chance(15))
					{
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
						{
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDot), 1000);
						}
					}
					if (Util.Chance(15))
					{
						if (Skoll_Haste_Debuff.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
						{
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastHasteDebuff), 1000);
						}
					}
				}
			}
			base.Think();
		}
		public int CastHasteDebuff(ECSGameTimer timer)
		{
			if (Body.TargetObject != null && HasAggro && Body.IsAlive)
			{
				Body.CastSpell(Skoll_Haste_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		public int CastDot(ECSGameTimer timer)
		{
			if (Body.TargetObject != null && HasAggro && Body.IsAlive)
			{
				Body.CastSpell(Skoll_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		private Spell m_Skoll_Haste_Debuff;
		private Spell Skoll_Haste_Debuff
		{
			get
			{
				if (m_Skoll_Haste_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 45;
					spell.ClientEffect = 5427;
					spell.Icon = 5427;
					spell.Name = "Skoll's Debuff Haste";
					spell.TooltipId = 5427;
					spell.Range = 1500;
					spell.Value = 38;
					spell.SpellID = 11811;
					spell.Target = "Enemy";
					spell.Type = eSpellType.CombatSpeedDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Skoll_Haste_Debuff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Skoll_Haste_Debuff);
				}
				return m_Skoll_Haste_Debuff;
			}
		}
		private Spell m_Skoll_Dot;
		private Spell Skoll_Dot
		{
			get
			{
				if (m_Skoll_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 20;
					spell.ClientEffect = 92;
					spell.Icon = 92;
					spell.Name = "Skoll's Poison";
					spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "You are covered in lava!";
					spell.Message2 = "{0} is covered in lava!";
					spell.Message3 = "The lava hardens and falls away.";
					spell.Message4 = "The lava falls from {0}'s skin.";
					spell.TooltipId = 92;
					spell.Range = 1500;
					spell.Damage = 80;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11812;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Skoll_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Skoll_Dot);
				}
				return m_Skoll_Dot;
			}
		}
	}
}

