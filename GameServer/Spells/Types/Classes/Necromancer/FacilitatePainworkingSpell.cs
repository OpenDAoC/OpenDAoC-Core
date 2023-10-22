using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Effects.Old;
using Core.GS.Skills;

namespace Core.GS.Spells;

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