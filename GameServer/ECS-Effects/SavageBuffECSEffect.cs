using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class SavageBuffECSGameEffect : StatBuffECSEffect
    {
        public SavageBuffECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            if (!IsActive)
            {
                ApplyBonus(Owner, (SpellHandler as AbstractSavageBuff).BonusCategory1,
                    (SpellHandler as AbstractSavageBuff).Property1, SpellHandler.Spell.Value, Effectiveness, false);
                
                // "You parry with extra skill!"
                // "{0} begins parrying faster!"
                OnEffectStartsMsg(true, false, true);
            }
            else
                OnHealthCost();
        }

        public override void OnStopEffect()
        {
            ApplyBonus(Owner, (SpellHandler as AbstractSavageBuff).BonusCategory1, (SpellHandler as AbstractSavageBuff).Property1, SpellHandler.Spell.Value, Effectiveness, true);
            OnHealthCost();
        }

        private void OnHealthCost()
        {
            if (SpellHandler.Spell.Power == 0)
                return;

            int cost;
            int maxHp = SpellHandler.Caster.MaxHealth;

            if (SpellHandler.Spell.Power < 0)
                cost = (int) (maxHp * Math.Abs(SpellHandler.Spell.Power) * 0.01);
            else
                cost = SpellHandler.Spell.Power;

            // Costs at least 1 HP, leaves at least 1 HP.
            cost = Math.Min(Math.Max(1, cost), Owner.Health - 1);

            // This can be negative if the owner is dead.
            if (cost > 0)
                Owner.ChangeHealth(Owner, eHealthChangeType.Spell, -cost);
        }
    }
}
