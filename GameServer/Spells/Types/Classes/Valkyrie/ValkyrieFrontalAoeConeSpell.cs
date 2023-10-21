using Core.GS.Effects;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	/// <summary>
	/// Handler to make the frontal pulsing cone show the effect animation on every pulse
	/// </summary>
	[SpellHandler("FrontalPulseConeDD")]
	public class ValkyrieFrontalAoeConeSpell : DirectDamageSpell
	{
		public ValkyrieFrontalAoeConeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override void OnSpellPulse(PulsingSpellEffect effect)
		{
			SendCastAnimation();
			base.OnSpellPulse(effect);
		}
	}
}
