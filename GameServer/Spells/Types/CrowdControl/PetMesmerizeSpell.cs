using Core.GS.AI.Brains;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("PetMesmerize")]
public class PetMesmerizeSpell : MesmerizeSpell
{
    public PetMesmerizeSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    public override void ApplyEffectOnTarget(GameLiving target)
    {
        if (!(target is IControlledBrain))
            return;
        base.ApplyEffectOnTarget(target);
    }
}