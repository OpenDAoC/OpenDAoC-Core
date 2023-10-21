namespace DOL.GS.SpellEffects
{
    public struct HealEffectComponent: IEffectComponent
    {
        public int Value;
        public GameLiving Target;
        public GameLiving Caster;
        public ESpellEffect Type { get; set; }
        public ushort SpellEffectId { get; set; }

        public HealEffectComponent(int value, GameLiving target,GameLiving caster, ushort spellEffectId)
        {
            this.Value = value;
            this.Target = target;
            this.Caster = caster;
            this.Type = ESpellEffect.Heal;
            this.SpellEffectId = spellEffectId;
        }
        
    }
}