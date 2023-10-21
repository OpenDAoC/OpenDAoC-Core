using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //shared timer 1

    [SpellHandler("MLFatDebuff")]
    public class InexorableDefeatSpell : MasterlevelDebuffHandling
    {
        public override EProperty Property1
        {
            get { return EProperty.FatigueConsumption; }
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect effect2 = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (effect2 != null)
            {
                effect2.Cancel(false);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
            base.OnEffectStart(effect);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public InexorableDefeatSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell,
            spellLine)
        {
        }
    }
}