namespace DOL.GS.SpellEffects
{
    public struct DamageEffectComponent : IEffectComponent
    {
        public int Value;
        public GameLiving Target;
        public GameLiving Caster;
        public eSpellEffect Type { get; set; }

        public eDamageType DamageType { get; set; }

        public ushort SpellEffectId { get; set; }

        public DamageEffectComponent(int value, eDamageType damageType, GameLiving target, GameLiving caster, ushort spellEffectId)
        {
            this.Value = value;
            this.Target = target;
            this.Caster = caster;
            this.Type = eSpellEffect.Damage;
            this.DamageType = damageType;
            this.SpellEffectId = spellEffectId;
        }
    }
}
