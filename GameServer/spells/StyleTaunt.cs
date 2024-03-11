using System;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Style taunt effect spell handler
    /// </summary>
    [SpellHandler("StyleTaunt")]
    public class StyleTaunt : SpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public StyleTaunt(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            return false;
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target is not GameNPC npc || npc.Brain is not IOldAggressiveBrain brain)
                return;

            // Style taunt and style detaunt default values are 17 and -19 respectively. The old formula multiplied that value to the (non-critical) damage, which resulted in excessively high threat changes.
            // It's unknown what the formula should actually be, or even if these value are correct.
            // Tooltips seem to indicate these are flat values, but that would make them close to useless at high level, so some kind of scaling is needed.
            // The value could scale based on the attack damage or user level.
            // If level scaling is chosen, attack speed normalization is something that could become necessary to prevent slow weapons from being penalized.
            // If damage scaling is chosen, stats, gear, and the target's armor would affect the result.
            // Detaunts are weirder in the sense that basing it on the damage would always lower the total threat, but basing it on the user level may lower total threat only if the damage is low enough.
            // This is a bit problematic because that would mean not attacking at all could sometimes be better to lose aggro, but this isn’t something the player could tell.
            // Long story short, keeping the damage based scaling seems easier and more intuitive from a player’s perspective, but with only a tenth of the spell value, and while taking critical damage into account.
            AttackData attackData = Caster.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);
            brain.AddToAggroList(Caster, (long) Math.Floor((attackData.Damage + attackData.CriticalDamage) * Spell.Value * 0.1));
        }
    }
}
