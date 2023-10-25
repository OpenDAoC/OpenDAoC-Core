using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.MasterLevels;

[SpellHandler("MLManadrain")]
public class PowerLeakSpell : MasterLevelSpellHandling
{
    public PowerLeakSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }

    public override void FinishSpellCast(GameLiving target)
    {
        m_caster.Mana -= PowerCost(target);
        base.FinishSpellCast(target);
    }

    public override void OnDirectEffect(GameLiving target)
    {
        if (target == null) return;
        if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

        //spell damage shood be 50-100 (thats the amount power tapped on use) i recommend 90 i think thats it but cood be wrong
        int mana = (int)(Spell.Damage);
        target.ChangeMana(target, EPowerChangeType.Spell, (-mana));

        target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
    }
}