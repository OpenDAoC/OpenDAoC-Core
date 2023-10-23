using System.Collections.Generic;
using Core.GS.AI;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

//shared timer 2

[SpellHandler("CoweringBellow")]
public class CoweringBellowSpell : FearSpell
{
    public override int CalculateSpellResistChance(GameLiving target)
    {
        return 0;
    }

    public override IList<GameLiving> SelectTargets(GameObject castTarget)
    {
        var list = new List<GameLiving>();
        GameLiving target = Caster;
        foreach (GameNpc npc in target.GetNPCsInRadius((ushort)Spell.Radius))
        {
            if (npc is GameNpc && npc.Brain is ControlledNpcBrain) //!(npc is NecromancerPet))
                list.Add(npc);
        }

        return list;
    }

    public CoweringBellowSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}