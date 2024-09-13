namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.PiercingMagic)]
    public class PiercingMagicSpellHandler : SpellHandler
    {
        public PiercingMagicSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new PiercingMagicECSGameEffect(initParams);
        }
    }
}
