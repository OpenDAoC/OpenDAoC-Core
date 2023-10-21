using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    [SpellHandler("Sabotage")]
    public class SabotageSpell : SpellHandler
    {
        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
            if (target is GameFont)
            {
                GameFont targetFont = target as GameFont;
                targetFont.Delete();
                MessageToCaster("Selected ward has been saboted!", EChatType.CT_SpellResisted);
            }
        }

        public SabotageSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}