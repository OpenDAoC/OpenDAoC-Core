namespace DOL.GS.SpellEffects
{
    public struct DamageEffectComponent : IEffectComponent
    {
        public int Value;
        public GameLiving Target;
        public GameLiving Caster;
        public ESpellEffect Type { get; set; }

        public EDamageType DamageType { get; set; }

        public ushort SpellEffectId { get; set; }

        public DamageEffectComponent(int value, EDamageType damageType, GameLiving target, GameLiving caster, ushort spellEffectId)
        {
            this.Value = value;
            this.Target = target;
            this.Caster = caster;
            this.Type = ESpellEffect.Damage;
            this.DamageType = damageType;
            this.SpellEffectId = spellEffectId;
        }
    }
}
