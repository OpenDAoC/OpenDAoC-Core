using DOL.AI.Brain;

namespace DOL.GS
{
    public class DiseaseECSGameEffect : ECSGameSpellEffect
    {
        private bool _critical;

        public DiseaseECSGameEffect(in ECSGameEffectInitParams initParams, bool critical) : base(initParams)
        {
            _critical = critical;
        }

        public override void OnStartEffect()
        {
            Owner.Disease(true);
            double baseSpeedDebuff = 0.15;
            double baseStrDebuff = 0.075;

            if (_critical)
                baseSpeedDebuff *= 2;

            Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, this, 1.0 - baseSpeedDebuff);
            Owner.BuffBonusMultCategory1.Set((int) eProperty.Strength, this, 1.0 - baseStrDebuff * Effectiveness);
            Owner.OnMaxSpeedChange();

            // "You are diseased!"
            // "{0} is diseased!"
            OnEffectStartsMsg(true, true, true);

            if (Owner is GameNPC npcOwner)
            {
                IOldAggressiveBrain aggroBrain = npcOwner.Brain as IOldAggressiveBrain;
                aggroBrain?.AddToAggroList(SpellHandler.Caster, 1);
            }
        }

        public override void OnStopEffect()
        {
            Owner.Disease(false);
            Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, this);
            Owner.BuffBonusMultCategory1.Remove((int) eProperty.Strength, this);

            // "You look healthy."
            // "{0} looks healthy again."
            OnEffectExpiresMsg(true, true, true);
            Owner.OnMaxSpeedChange();
        }
    }
}
