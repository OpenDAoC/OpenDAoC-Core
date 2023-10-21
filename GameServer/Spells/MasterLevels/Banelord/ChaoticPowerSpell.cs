namespace DOL.GS.Spells
{
    //shared timer 3

    [SpellHandler("BLToHit")]
    public class ChaoticPowerSpell : MasterLevelBuffHandling
    {
        public override EProperty Property1
        {
            get { return EProperty.ToHitBonus; }
        }

        // constructor
        public ChaoticPowerSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}