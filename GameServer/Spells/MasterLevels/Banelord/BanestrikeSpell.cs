using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //no shared timer
    #region Banelord-10
    [SpellHandler("Banespike")]
    public class BanestrikeSpell : MasterLevelBuffHandling
    {
        public override EProperty Property1 { get { return EProperty.MeleeDamage; } }

        public BanestrikeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}