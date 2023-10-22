using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("HealFlask")]
public class HealFlaskSpell : SpellHandler
{
    public HealFlaskSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
    {
    }

    public override bool HasPositiveEffect
    {
        get { return true; }
    }
}

/// <summary>
/// Handler for Flask use2: Target gets chances not to die from the last hit.
/// </summary>
[SpellHandler("DeadFlask")]
public class DeadFlaskSpell : SpellHandler
{
    public DeadFlaskSpell(GameLiving caster, Spell spell, SpellLine spellLine)
        : base(caster, spell, spellLine)
    {
    }

    public override bool HasPositiveEffect
    {
        get { return true; }
    }
}