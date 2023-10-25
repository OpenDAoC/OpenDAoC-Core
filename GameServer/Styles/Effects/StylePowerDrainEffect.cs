using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Styles;

[SpellHandler("StylePowerDrain")]
public class StylePowerDrainEffect : DamageToPowerSpell
{
	public override int CalculateSpellResistChance(GameLiving target)
	{
		return 0;
	}
	
	public override void OnDirectEffect(GameLiving target)
    {
        base.OnDirectEffect(target);
		SendEffectAnimation(target, 0, false, 1);
    }

	// constructor
	public StylePowerDrainEffect(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}