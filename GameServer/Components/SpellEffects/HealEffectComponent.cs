namespace DOL.GS.SpellEffects
{
    public struct HealEffectComponent: IEffectComponent
    {
        public int Value;
        public GameLiving Target;
        public GameLiving Caster;
        public eSpellEffect Type { get; set; }

        public HealEffectComponent(int value, GameLiving target,GameLiving caster)
        {
            this.Value = value;
            this.Target = target;
            this.Caster = caster;
            this.Type = eSpellEffect.Heal;
        }
        
    }
}