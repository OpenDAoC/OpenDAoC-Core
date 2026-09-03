using System;

namespace DOL.GS
{
    public class SavageBuffECSGameEffect : StatBuffECSEffect
    {
        public SavageBuffECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStopEffect()
        {
            base.OnStopEffect();
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
