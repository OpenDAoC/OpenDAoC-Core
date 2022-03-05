using DOL.GS.Spells;
using DOL.GS.PacketHandler;

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
            Owner.DebuffCategory[(int)eProperty.ArcheryRange] += (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[(int)eProperty.SpellRange] += (int)SpellHandler.Spell.Value;
            //Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
            (SpellHandler as NearsightSpellHandler).SendEffectAnimation(Owner, 0, false, 1);
            
            // "Your combat skills are hampered by blindness!"
            // "{0} stumbles, unable to see!"
            OnEffectStartsMsg(Owner, true, true, true);

        }

        public override void OnStopEffect()
        {
            // percent category
            Owner.DebuffCategory[(int)eProperty.ArcheryRange] -= (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[(int)eProperty.SpellRange] -= (int)SpellHandler.Spell.Value;

            // "Your vision returns to normal."
            // "The blindness recedes from {0}."
            OnEffectExpiresMsg(Owner, true, false, true);

        }
    }
}