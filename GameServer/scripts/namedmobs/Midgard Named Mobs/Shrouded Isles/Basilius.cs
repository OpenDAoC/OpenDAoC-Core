using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Basilius : GameEpicBoss
	{
		public Basilius() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Basilius Initializing...");
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158315);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			BasiliusBrain sbrain = new BasiliusBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Bleed))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(BasiliusBleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
			{
				if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && ad.Target.IsAlive)
					CastSpell(Basilius_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
        #region Spells
        private Spell m_Basilius_stun;
		private Spell Basilius_stun
		{
			get
			{
				if (m_Basilius_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2165;
					spell.Icon = 2132;
					spell.TooltipId = 2132;
					spell.Duration = 6;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 400;
					spell.SpellID = 11893;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					m_Basilius_stun = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Basilius_stun);
				}
				return m_Basilius_stun;
			}
		}
		private Spell m_BasiliusBleed;
		private Spell BasiliusBleed
		{
			get
			{
				if (m_BasiliusBleed == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2130;
					spell.Icon = 3411;
					spell.TooltipId = 3411;
					spell.Damage = 100;
					spell.Name = "Bleed";
					spell.Description = "Does 100 bleeding damage to a target every 3 seconds for 30 seconds.";
					spell.Message1 = "You are bleeding! ";
					spell.Message2 = "{0} is bleeding! ";
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 350;
					spell.SpellID = 11892;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.StyleBleeding.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_BasiliusBleed = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BasiliusBleed);
				}
				return m_BasiliusBleed;
			}
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class BasiliusBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BasiliusBrain() : base()
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
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "BasiliusBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}
	}
}

