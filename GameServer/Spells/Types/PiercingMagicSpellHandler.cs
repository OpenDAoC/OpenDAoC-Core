namespace DOL.GS.Spells
{
    [SpellHandler("PiercingMagic")]
    public class PiercingMagicSpellHandler : SpellHandler
    {
        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
            new PiercingMagicEcsSpellEffect(initParams);
        }
        // constructor
        public PiercingMagicSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }   
}
