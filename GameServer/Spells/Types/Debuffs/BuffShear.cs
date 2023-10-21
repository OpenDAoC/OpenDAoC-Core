using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.Spells
{
	/// <summary>
	/// Shears strength buff 
	/// </summary>
	[SpellHandler("StrengthShear")]
	public class StrengthShearSpell : ABuffShear
	{
		public override string ShearSpellType { get	{ return "StrengthBuff"; } }
		public override string DelveSpellType { get { return "Strength"; } }

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
            GameSpellEffect effect;
            effect = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (effect != null)
            {
                effect.Cancel(false);
                return;
            }
        }

		// constructor
		public StrengthShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Shears dexterity buff
	/// </summary>
	[SpellHandler("DexterityShear")]
	public class DexterityShearSpell : ABuffShear
	{
		public override string ShearSpellType { get	{ return "DexterityBuff"; } }
		public override string DelveSpellType { get { return "Dexterity"; } }
		// constructor
		public DexterityShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Shears constitution buff
	/// </summary>
	[SpellHandler("ConstitutionShear")]
	public class ConstitutionShearSpell : ABuffShear
	{
		public override string ShearSpellType { get	{ return "ConstitutionBuff"; } }
		public override string DelveSpellType { get { return "Constitution"; } }
		// constructor
		public ConstitutionShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Shears acuity buff
	/// </summary>
	[SpellHandler("AcuityShear")]
	public class AcuityShearSpell : ABuffShear
	{
		public override string ShearSpellType { get	{ return "AcuityBuff"; } }
		public override string DelveSpellType { get { return "Acuity"; } }
		// constructor
		public AcuityShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Shears str/con buff
	/// </summary>
	[SpellHandler("StrengthConstitutionShear")]
	public class StrConShearSpell : ABuffShear
	{
		public override string ShearSpellType { get	{ return "StrengthConstitutionBuff"; } }
		public override string DelveSpellType { get { return "Str/Con"; } }
		// constructor
		public StrConShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Shears dex/qui buff
	/// </summary>
	[SpellHandler("DexterityQuicknessShear")]
	public class DexQuiShearSpell : ABuffShear
	{
		public override string ShearSpellType { get	{ return "DexterityQuicknessBuff"; } }
		public override string DelveSpellType { get { return "Dex/Qui"; } }
		// constructor
		public DexQuiShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("RandomBuffShear")]
	public class RandomBuffShearSpell : SpellHandler
	{

		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void OnDirectEffect(GameLiving target)
		{
			base.OnDirectEffect(target);
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
			if (target is GameNpc)
			{
				GameNpc npc = (GameNpc)target;
				IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, 1);
			}

			//check for spell.
			foreach (GameSpellEffect effect in target.EffectList.GetAllOfType<GameSpellEffect>())
			{
				foreach (Type buffType in buffs)
				{
					if (effect.SpellHandler.GetType().Equals(buffType))
					{
						SendEffectAnimation(target, 0, false, 1);
						effect.Cancel(false);
						MessageToCaster("Your spell rips away some of your target's enhancing magic.", EChatType.CT_Spell);
						MessageToLiving(target, "Some of your enhancing magic has been ripped away by a spell!", EChatType.CT_Spell);
						return;
					}
				}
			}

			SendEffectAnimation(target, 0, false, 0);
			MessageToCaster("No enhancement of that type found on the target.", EChatType.CT_SpellResisted);

			/*
			if (!noMessages) 
			{
				MessageToLiving(effect.Owner, effect.Spell.Message3, eChatType.CT_SpellExpires);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(effect.Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}
			*/
		}

		private static Type[] buffs = new Type[] { typeof(AcuityBuff), typeof(StrengthBuff), typeof(DexterityBuff), typeof(ConstitutionBuff), typeof(StrConBuff), typeof(DexQuiBuff),
			typeof(ArmorFactorBuff),typeof(ArmorAbsorptionBuff),typeof(HealthRegenSpell),typeof(CombatSpeedBuff),typeof(PowerRegenSpellHandler),typeof(UninterruptableSpell),typeof(WeaponSkillBuff),typeof(DPSBuff),typeof(EvadeChanceBuff),typeof(ParryChanceBuff),
			typeof(ColdResistBuff),typeof(EnergyResistBuff),typeof(CrushResistBuff),typeof(ThrustResistBuff),typeof(SlashResistBuff),typeof(MatterResistBuff),typeof(BodyResistBuff),typeof(HeatResistBuff),typeof(SpiritResistBuff),typeof(BodySpiritEnergyBuff),typeof(HeatColdMatterBuff),typeof(CrushSlashThrustBuff),
			typeof(EnduranceRegenSpell),typeof(DamageAddSpell),typeof(DamageShieldSpell) };
		// constructor
		public RandomBuffShearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
