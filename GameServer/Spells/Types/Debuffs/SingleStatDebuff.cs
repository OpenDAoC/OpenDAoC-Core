using System;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Spells
{
	/// <summary>
	/// Debuffs a single stat
	/// </summary>
	public abstract class SingleStatDebuff : SingleStatBuff
	{
		// bonus category
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.Debuff; } }

        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
			new StatDebuffEcsSpellEffect(initParams);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
		{
			// var debuffs = target.effectListComponent.GetSpellEffects()
			// 					.Where(x => x.SpellHandler is SingleStatDebuff);

			// foreach (var debuff in debuffs)
            // {
			// 	var debuffSpell = debuff.SpellHandler as SingleStatDebuff;

			// 	if (debuffSpell.Property1 == this.Property1 && debuffSpell.Spell.Value >= Spell.Value)
			// 	{
			// 		// Old Spell is Better than new one
			// 		SendSpellResistAnimation(target);
			// 		this.MessageToCaster(eChatType.CT_SpellResisted, "{0} already has that effect.", target.GetName(0, true));
			// 		MessageToCaster("Wait until it expires. Spell Failed.", eChatType.CT_SpellResisted);
			// 		// Prevent Adding.
			// 		return;
			// 	}
            // }


			base.ApplyEffectOnTarget(target);
			
			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
			}
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
			}
			//if(target is GameNPC) 
			//{
			//	IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
			//	if (aggroBrain != null)
			//		aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			//}
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
			duration -= duration * target.GetResist(Spell.DamageType) * 0.01;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);
			return (int)duration;
		}
		
		/// <summary>
		/// Calculates chance of spell getting resisted
		/// </summary>
		/// <param name="target">the target of the spell</param>
		/// <returns>chance that spell will be resisted for specific target</returns>		
        public override int CalculateSpellResistChance(GameLiving target)
        {
        	int basechance =  base.CalculateSpellResistChance(target);      
            /*
 			GameSpellEffect rampage = SpellHandler.FindEffectOnTarget(target, "Rampage");
            if (rampage != null)
            {
            	basechance += (int)rampage.Spell.Value;
            }*/
            return Math.Min(100, basechance);
        }
		// constructor
		public SingleStatDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Str stat baseline debuff
	/// </summary>
	[SpellHandler("StrengthDebuff")]
	public class StrDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Strength; } }

		// constructor
		public StrDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Dex stat baseline debuff
	/// </summary>
	[SpellHandler("DexterityDebuff")]
	public class DexDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Dexterity; } }	

		// constructor
		public DexDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Con stat baseline debuff
	/// </summary>
	[SpellHandler("ConstitutionDebuff")]
	public class ConDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Constitution; } }	

		// constructor
		public ConDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Armor factor debuff
	/// </summary>
	[SpellHandler("ArmorFactorDebuff")]
	public class ArmorFactorDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.ArmorFactor; } }	

		// constructor
		public ArmorFactorDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Armor Absorption debuff
	/// </summary>
	[SpellHandler("ArmorAbsorptionDebuff")]
	public class ArmorAbsorptionDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.ArmorAbsorption; } }

		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public ArmorAbsorptionDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Combat Speed debuff
	/// </summary>
	[SpellHandler("CombatSpeedDebuff")]
	public class CombatSpeedDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.MeleeSpeed; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public CombatSpeedDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Melee damage debuff
	/// </summary>
	[SpellHandler("MeleeDamageDebuff")]
	public class MeleeDamageDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.MeleeDamage; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public MeleeDamageDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Fatigue reduction debuff
	/// </summary>
	[SpellHandler("FatigueConsumptionDebuff")]
	public class FatigueConsumptionDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.FatigueConsumption; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public FatigueConsumptionDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Fumble chance debuff
	/// </summary>
	[SpellHandler("FumbleChanceDebuff")]
	public class FumbleChanceDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.FumbleChance; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public FumbleChanceDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	
	/// <summary>
	/// DPS debuff
	/// </summary>
	[SpellHandler("DPSDebuff")]
	public class DpsDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.DPS; } }	

		// constructor
		public DpsDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// Skills Debuff
	/// </summary>
	[SpellHandler("SkillsDebuff")]
	public class SkillsDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.AllSkills; } }	

		// constructor
		public SkillsDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// Acuity stat baseline debuff
	/// </summary>
	[SpellHandler("AcuityDebuff")]
	public class AcuityDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Acuity; } }	

		// constructor
		public AcuityDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// Quickness stat baseline debuff
	/// </summary>
	[SpellHandler("QuicknessDebuff")]
	public class QuiDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Quickness; } }	

		// constructor
		public QuiDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// ToHit Skill debuff
	/// </summary>
	[SpellHandler("ToHitDebuff")]
	public class ToHitSkillDebuffSpell : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.ToHitBonus; } }	

		// constructor
		public ToHitSkillDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
 }
