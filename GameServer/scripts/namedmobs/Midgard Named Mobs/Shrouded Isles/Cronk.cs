using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Cronk : GameEpicBoss
	{
		public Cronk() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Cronk Initializing...");
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
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159504);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CronkBrain sbrain = new CronkBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(70))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(CronkDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DexQuiDebuff))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(DebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
        #region Spells
        private Spell m_CronkDD;
		private Spell CronkDD
		{
			get
			{
				if (m_CronkDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 20;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Fire Blast";
					spell.Range = 500;
					spell.SpellID = 11881;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_CronkDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CronkDD);
				}
				return m_CronkDD;
			}
		}
		private Spell m_DebuffDQ;
		private Spell DebuffDQ
		{
			get
			{
				if (m_DebuffDQ == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(25, 45);
					spell.Duration = 60;
					spell.Value = 78;
					spell.ClientEffect = 2627;
					spell.Icon = 2627;
					spell.TooltipId = 2627;
					spell.Name = "Vitality Dispersal";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11882;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DexterityQuicknessDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_DebuffDQ = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DebuffDQ);
				}
				return m_DebuffDQ;
			}
		}
        #endregion
    }
}
namespace DOL.AI.Brain
{
	public class CronkBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CronkBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "CronkBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}		
	}
}

