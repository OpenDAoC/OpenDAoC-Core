using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.Spells;

/// <summary>
/// Summary description for TurretPBAoESpellHandler.
/// </summary>
[SpellHandler("TurretPBAoE")]
public class PetPbaoeSpell : DirectDamageSpell
{
    public PetPbaoeSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

    public override bool HasPositiveEffect => false;

    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        // Allow the PBAoE to be casted on the main turret only.
        Target = Caster.ControlledBrain?.Body;
        return base.CheckBeginCast(Target);
    }

    public override void ApplyEffectOnTarget(GameLiving target)
    {
        if (Target.IsAlive)
            base.ApplyEffectOnTarget(target);
    }

    public override void DamageTarget(AttackData ad, bool showEffectAnimation)
    {
        // Set the turret as the attacker so that aggro is split properly (damage should already be calculated at this point).
        // This may cause some issues if something else relies on 'ad.Attacker', but is better than calculating aggro here.
        ad.Attacker = Target;
        base.DamageTarget(ad, showEffectAnimation);
    }
}