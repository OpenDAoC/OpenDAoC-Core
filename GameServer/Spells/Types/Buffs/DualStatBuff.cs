using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	/// <summary>
	/// Buffs two stats at once, goes into specline bonus category
	/// </summary>	
	public abstract class DualStatBuff : SingleStatBuff
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.SpecBuff; } }
		public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.SpecBuff; } }

		/// <summary>
		/// Default Constructor
		/// </summary>
		protected DualStatBuff(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
		}
	}

	/// <summary>
	/// Str/Con stat specline buff
	/// </summary>
	[SpellHandler("StrengthConstitutionBuff")]
	public class StrConBuff : DualStatBuff
	{
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(AbilityConstants.VampiirStrength)
        	   || target.HasAbility(AbilityConstants.VampiirConstitution))
            {
                MessageToCaster("Your target already has an effect of that type!", EChatType.CT_Spell);
                return;
            }
            base.ApplyEffectOnTarget(target);
        }
		public override EProperty Property1 { get { return EProperty.Strength; } }	
		public override EProperty Property2 { get { return EProperty.Constitution; } }	

		// constructor
		public StrConBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Dex/Qui stat specline buff
	/// </summary>
	[SpellHandler("DexterityQuicknessBuff")]
	public class DexQuiBuff : DualStatBuff
	{
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(AbilityConstants.VampiirDexterity)
        	   || target.HasAbility(AbilityConstants.VampiirQuickness))
            {
                MessageToCaster("Your target already has an effect of that type!", EChatType.CT_Spell);
                return;
            }
            base.ApplyEffectOnTarget(target);
        }
		public override EProperty Property1 { get { return EProperty.Dexterity; } }	
		public override EProperty Property2 { get { return EProperty.Quickness; } }	

		// constructor
		public DexQuiBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
