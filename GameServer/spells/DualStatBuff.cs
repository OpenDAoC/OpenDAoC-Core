using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Buffs two stats at once, goes into specline bonus category
    /// </summary>	
    public abstract class DualStatBuff : SingleStatBuff
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.SpecBuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.SpecBuff;

        protected DualStatBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler(eSpellType.StrengthConstitutionBuff)]
    public class StrengthConBuff : DualStatBuff
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.SpecBuff;

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirStrength) || target.HasAbility(Abilities.VampiirConstitution))
            {
                MessageToCaster("Your target already has an effect of that type!", eChatType.CT_Spell);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        public override eProperty Property1 => eProperty.Strength;
        public override eProperty Property2 => eProperty.Constitution;

        public StrengthConBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler(eSpellType.DexterityQuicknessBuff)]
    public class DexterityQuiBuff : DualStatBuff
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.SpecBuff;

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirDexterity) || target.HasAbility(Abilities.VampiirQuickness))
            {
                MessageToCaster("Your target already has an effect of that type!", eChatType.CT_Spell);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        public override eProperty Property1 => eProperty.Dexterity;
        public override eProperty Property2 => eProperty.Quickness;

        public DexterityQuiBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
    }
}
