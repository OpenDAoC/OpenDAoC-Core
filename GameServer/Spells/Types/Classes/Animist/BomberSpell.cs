using System;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("Bomber")]
public class BomberSpell : SummonSpellHandler
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BomberSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
        m_isSilent = true;
    }

    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        if (Spell.SubSpellID == 0)
        {
            MessageToCaster("SPELL NOT IMPLEMENTED: CONTACT GM", EChatType.CT_Important);
            return false;
        }

        return base.CheckBeginCast(selectedTarget);
    }

    public override void ApplyEffectOnTarget(GameLiving target)
    {
        base.ApplyEffectOnTarget(target);

        if (m_pet is not null)
        {
            m_pet.Level = m_pet.Owner?.Level ?? 1; // No bomber class to override SetPetLevel() in, so set level here.
            m_pet.Name = Spell.Name;
            m_pet.Flags ^= ENpcFlags.DONTSHOWNAME;
            m_pet.Flags ^= ENpcFlags.PEACE;
            m_pet.FixedSpeed = true;
            m_pet.MaxSpeedBase = 350;
            m_pet.TargetObject = target;
            m_pet.Follow(target, 5, Spell.Range * 5);
        }
    }

    protected override IControlledBrain GetPetBrain(GameLiving owner)
    {
        return new BomberBrain(owner, Spell, SpellLine);
    }

    protected override void SetBrainToOwner(IControlledBrain brain) { }

    protected override void OnNpcReleaseCommand(CoreEvent e, object sender, EventArgs arguments) { }

    public override void CastSubSpells(GameLiving target) { }
}