using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class SavageBuffEcsEffect : StatBuffEcsSpellEffect
    {
        public SavageBuffEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            if (!IsBuffActive && !IsDisabled)
            {
                ApplyBonus(Owner, (SpellHandler as ASavageBuff).BonusCategory1,
                    (SpellHandler as ASavageBuff).Property1, SpellHandler.Spell.Value, Effectiveness, false);
                
                // "You parry with extra skill!"
                // "{0} begins parrying faster!"
                OnEffectStartsMsg(Owner, true, false, true);
            }
            else
                OnHealthCost();
        }

        public override void OnStopEffect()
        {
            ApplyBonus(Owner, (SpellHandler as ASavageBuff).BonusCategory1, (SpellHandler as ASavageBuff).Property1, SpellHandler.Spell.Value, Effectiveness, true);
            OnHealthCost();           
        }

        private void OnHealthCost()
        {
            if (SpellHandler.Spell.Power != 0)
            {
                int cost = 0;
                if (SpellHandler.Spell.Power < 0)
                    cost = (int)(SpellHandler.Caster.MaxHealth * Math.Abs(SpellHandler.Spell.Power) * 0.01);
                else
                    cost = SpellHandler.Spell.Power;
                if (Owner.Health > cost)
                    Owner.ChangeHealth(Owner, EHealthChangeType.Spell, -cost);
            }
        }
    }
}
