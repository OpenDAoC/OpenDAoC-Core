using Core.GS.Effects;

namespace Core.GS.Spells
{
	/// <summary>
	/// Spell handler for Facilitate Painworking.
	/// </summary>
	/// <author>Aredhel</author>
	[SpellHandler("FacilitatePainworking")]
	class FacilitatePainworkingSpell : SpellHandler
	{
		public FacilitatePainworkingSpell(GameLiving caster, Spell spell, SpellLine line) 
			: base(caster, spell, line) 
        {
        }
		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new FacilitatePainworkingEcsSpellEffect(initParams);
		}
		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new FacilitatePainworkingEffect(this,
                CalculateEffectDuration(target, effectiveness), 0, effectiveness);
        }
	}
}
