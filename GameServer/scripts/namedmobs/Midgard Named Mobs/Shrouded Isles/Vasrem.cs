using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Vasrem : GameEpicBoss
	{
		public Vasrem() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Vasrem Initializing...");
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
        public override void StartAttack(GameObject target)//mob only casting
        {
        }
        public override int MaxHealth
		{
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167546);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			VasremBrain sbrain = new VasremBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void DealDamage(AttackData ad)
        {
			if(ad != null && ad.DamageType == eDamageType.Body && ad.Target != null && ad.Target.IsAlive)
				Health += ad.Damage;
			base.DealDamage(ad);
        }
    }
}
namespace DOL.AI.Brain
{
	public class VasremBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public VasremBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(Vasrem_Lifetap))
					Body.Spells.Add(Vasrem_Lifetap);
				if (!Body.Spells.Contains(VasremDebuffDQ))
					Body.Spells.Add(VasremDebuffDQ);
				if (!Body.Spells.Contains(VasremSCDebuff))
					Body.Spells.Add(VasremSCDebuff);
			}
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if(HasAggro && Body.TargetObject != null)
            {
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

							Body.TurnTo(Body.TargetObject);
							if (Util.Chance(100))
							{
								GameLiving target = Body.TargetObject as GameLiving;
								if (target != null)
								{
									if (!Body.IsCasting && Body.GetSkillDisabledDuration(VasremSCDebuff) == 0 && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StrConDebuff))
										Body.CastSpell(VasremSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else if (!Body.IsCasting && Body.GetSkillDisabledDuration(VasremDebuffDQ) == 0 && !target.effectListComponent.ContainsEffectForEffectType(eEffect.DexQuiDebuff))
										Body.CastSpell(VasremDebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else
										Body.CastSpell(Vasrem_Lifetap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
						}
					}
				}
			}
			base.Think();
		}
        #region Spells
        private Spell m_Vasrem_Lifetap;
		private Spell Vasrem_Lifetap
		{
			get
			{
				if (m_Vasrem_Lifetap == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 4;
					spell.RecastDelay = 0;
					spell.ClientEffect = 9191;
					spell.Icon = 710;
					spell.Damage = 450;
					spell.Name = "Drain Life Essence";
					spell.Range = 1800;
					spell.SpellID = 11886;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.MoveCast = true;
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Body; //Body DMG Type
					m_Vasrem_Lifetap = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Vasrem_Lifetap);
				}
				return m_Vasrem_Lifetap;
			}
		}
		private Spell m_VasremSCDebuff;
		private Spell VasremSCDebuff
		{
			get
			{
				if (m_VasremSCDebuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 60;
					spell.ClientEffect = 5408;
					spell.Icon = 5408;
					spell.Name = "S/C Debuff";
					spell.TooltipId = 5408;
					spell.Range = 1200;
					spell.Value = 65;
					spell.Duration = 60;
					spell.SpellID = 11887;
					spell.Target = "Enemy";
					spell.Type = "StrengthConstitutionDebuff";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_VasremSCDebuff = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VasremSCDebuff);
				}
				return m_VasremSCDebuff;
			}
		}
		private Spell m_VasremDebuffDQ;
		private Spell VasremDebuffDQ
		{
			get
			{
				if (m_VasremDebuffDQ == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 60;
					spell.Duration = 60;
					spell.Value = 65;
					spell.ClientEffect = 2627;
					spell.Icon = 2627;
					spell.TooltipId = 2627;
					spell.Name = "D/Q Debuff";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11888;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DexterityQuicknessDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_VasremDebuffDQ = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VasremDebuffDQ);
				}
				return m_VasremDebuffDQ;
			}
		}
		#endregion
	}
}

