using DOL.GS.Spells;

namespace DOL.GS
{
    public class NearsightECSGameEffect : ECSGameSpellEffect
    {
        public NearsightECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            // percent category
            Owner.DebuffCategory[eProperty.ArcheryRange] += (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[eProperty.SpellRange] += (int)SpellHandler.Spell.Value;
            //Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
            (SpellHandler as NearsightSpellHandler).SendEffectAnimation(Owner, 0, false, 1);
            
            // "Your combat skills are hampered by blindness!"
            // "{0} stumbles, unable to see!"
            OnEffectStartsMsg(true, true, true);

        }

        public override void OnStopEffect()
        {
            // percent category
            Owner.DebuffCategory[eProperty.ArcheryRange] -= (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[eProperty.SpellRange] -= (int)SpellHandler.Spell.Value;

            // "Your vision returns to normal."
            // "The blindness recedes from {0}."
            OnEffectExpiresMsg(true, false, true);

        }
    }
}