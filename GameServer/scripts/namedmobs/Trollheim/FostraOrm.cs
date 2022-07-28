using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;

namespace DOL.GS
{
	public class FostraOrm : GameEpicNPC
	{
		public FostraOrm() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Fostra Orm Initializing...");
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
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 8000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161054);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			FostraOrmBrain sbrain = new FostraOrmBrain();
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
	public class FostraOrmBrain : StandardMobBrain
	{
		public FostraOrmBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				if (Util.Chance(25) && target != null)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
					{
						Body.CastSpell(OrmDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
				if (Util.Chance(25) && target != null)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
					{
						Body.CastSpell(OrmDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
			}
			base.Think();
		}
		private Spell m_OrmDot;
		private Spell OrmDot
		{
			get
			{
				if (m_OrmDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3411;
					spell.Icon = 3411;
					spell.TooltipId = 3411;
					spell.Name = "Orm Poison";
					spell.Description = "Inflicts 70 damage to the target every 3 sec for 30 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.Damage = 70;
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 500;
					spell.SpellID = 11853;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.DamageType = (int)eDamageType.Body;
					spell.Uninterruptible = true;
					m_OrmDot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OrmDot);
				}
				return m_OrmDot;
			}
		}
		private Spell m_OrmDisease;
		private Spell OrmDisease
		{
			get
			{
				if (m_OrmDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4375;
					spell.Icon = 4375;
					spell.Name = "Disease";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 4375;
					spell.Range = 350;
					spell.Duration = 120;
					spell.SpellID = 11854;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_OrmDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OrmDisease);
				}
				return m_OrmDisease;
			}
		}
	}
}
