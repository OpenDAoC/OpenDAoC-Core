namespace Core.GS.Spells
{
    [SpellHandler("KeepDamageBuff")]
    public class FaultfinderSpell : MasterLevelBuffHandling
    {
        public override EProperty Property1
        {
            get { return EProperty.KeepDamage; }
        }

        public FaultfinderSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}