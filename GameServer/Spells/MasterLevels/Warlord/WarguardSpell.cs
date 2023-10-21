namespace Core.GS.Spells
{
    //shared timer 5
    #region Warlord-10
    [SpellHandler("MLABSBuff")]
    public class WarguardSpell : MasterLevelBuffHandling
    {
        public override EProperty Property1 { get { return EProperty.ArmorAbsorption; } }

        public WarguardSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}