using Core.GS.Spells;

namespace Core.GS
{
    public class NearsightEcsSpellEffect : EcsGameSpellEffect
    {
        public NearsightEcsSpellEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            // percent category
            Owner.DebuffCategory[(int)EProperty.ArcheryRange] += (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[(int)EProperty.SpellRange] += (int)SpellHandler.Spell.Value;
            //Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
            (SpellHandler as NearsightSpell).SendEffectAnimation(Owner, 0, false, 1);
            
            // "Your combat skills are hampered by blindness!"
            // "{0} stumbles, unable to see!"
            OnEffectStartsMsg(Owner, true, true, true);

        }

        public override void OnStopEffect()
        {
            // percent category
            Owner.DebuffCategory[(int)EProperty.ArcheryRange] -= (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[(int)EProperty.SpellRange] -= (int)SpellHandler.Spell.Value;

            // "Your vision returns to normal."
            // "The blindness recedes from {0}."
            OnEffectExpiresMsg(Owner, true, false, true);

        }
    }
}