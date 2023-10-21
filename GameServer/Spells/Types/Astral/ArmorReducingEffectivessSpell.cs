using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    /// <summary>
    /// A proc to lower target's ArmorFactor and ArmorAbsorption.
    /// </summary>
    [SpellHandler("ArmorReducingEffectiveness")]
    public class ArmorReducingEffectivenessSpell : DualStatDebuff
    {
        public override EProperty Property1
        {
            get { return EProperty.ArmorFactor; }
        }

        public override EProperty Property2
        {
            get { return EProperty.ArmorAbsorption; }
        }

        public ArmorReducingEffectivenessSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell,
            line)
        {
        }
    }
}