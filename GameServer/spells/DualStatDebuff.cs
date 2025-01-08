namespace DOL.GS.Spells
{
    /// <summary>
    /// Debuffs two stats at once, goes into specline bonus category
    /// </summary>
    public abstract class DualStatDebuff : SingleStatDebuff
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.Debuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.Debuff;

        public DualStatDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler(eSpellType.StrengthConstitutionDebuff)]
    public class StrengthConDebuff : DualStatDebuff
    {
        public override eProperty Property1 => eProperty.Strength;
        public override eProperty Property2 => eProperty.Constitution;

        public StrengthConDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler(eSpellType.DexterityQuicknessDebuff)]
    public class DexterityQuiDebuff : DualStatDebuff
    {
        public override eProperty Property1 => eProperty.Dexterity;
        public override eProperty Property2 => eProperty.Quickness;

        public DexterityQuiDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler(eSpellType.DexterityConstitutionDebuff)]
    public class DexterityConDebuff : DualStatDebuff
    {
        public override eProperty Property1 => eProperty.Dexterity;
        public override eProperty Property2 => eProperty.Constitution;

        public DexterityConDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler(eSpellType.WeaponSkillConstitutionDebuff)]
    public class WeaponskillConDebuff : DualStatDebuff
    {
        public override eProperty Property1 => eProperty.WeaponSkill;
        public override eProperty Property2 => eProperty.Constitution;

        public WeaponskillConDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
