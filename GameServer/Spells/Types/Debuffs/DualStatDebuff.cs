using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	/// <summary>
	/// Debuffs two stats at once, goes into specline bonus category
	/// </summary>	
	public abstract class DualStatDebuff : SingleStatDebuff
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.Debuff; } }
		public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.Debuff; } }

        // public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        // {
		// 	var debuffs = target.effectListComponent.GetSpellEffects()
		// 						.Where(x => x.SpellHandler is DualStatDebuff);

		// 	foreach (var debuff in debuffs)
		// 	{
		// 		var debuffSpell = debuff.SpellHandler as DualStatDebuff;

		// 		if (debuffSpell.Property1 == this.Property1 && debuffSpell.Property2 == this.Property2 && debuffSpell.Spell.Value >= Spell.Value)
		// 		{
		// 			// Old Spell is Better than new one
		// 			SendSpellResistAnimation(target);
		// 			this.MessageToCaster(eChatType.CT_SpellResisted, "{0} already has that effect.", target.GetName(0, true));
		// 			MessageToCaster("Wait until it expires. Spell Failed.", eChatType.CT_SpellResisted);
		// 			// Prevent Adding.
		// 			return;
		// 		}
		// 	}

		// 	base.ApplyEffectOnTarget(target, effectiveness);
		// }

        // constructor
        public DualStatDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	/// <summary>
	/// Str/Con stat specline debuff
	/// </summary>
	[SpellHandler("StrengthConstitutionDebuff")]
	public class StrConDebuff : DualStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Strength; } }
		public override EProperty Property2 { get { return EProperty.Constitution; } }

		// constructor
		public StrConDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	/// <summary>
	/// Dex/Qui stat specline debuff
	/// </summary>
	[SpellHandler("DexterityQuicknessDebuff")]
	public class DexQuiDebuff : DualStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Dexterity; } }
		public override EProperty Property2 { get { return EProperty.Quickness; } }

		// constructor
		public DexQuiDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
	/// Dex/Con Debuff for assassin poisons
	/// <summary>
	/// Dex/Con stat specline debuff
	/// </summary>
	[SpellHandler("DexterityConstitutionDebuff")]
	public class DexConDebuff : DualStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Dexterity; } }
		public override EProperty Property2 { get { return EProperty.Constitution; } }

		// constructor
		public DexConDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	[SpellHandler("WeaponSkillConstitutionDebuff")]
	public class WeaponskillConDebuff : DualStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.WeaponSkill; } }
		public override EProperty Property2 { get { return EProperty.Constitution; } }
		public WeaponskillConDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
