using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;

namespace DOL.GS
{
    public class DiseaseECSGameEffect : ECSGameSpellEffect
    {
        public DiseaseECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            Owner.Disease(true);
            Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, EffectType, 1.0 - 0.15);
            Owner.BuffBonusMultCategory1.Set((int)eProperty.Strength, EffectType, 1.0 - 0.075);

            (SpellHandler as DiseaseSpellHandler).SendUpdates(this);

            // "You are diseased!"
            // "{0} is diseased!"
            OnEffectStartsMsg(Owner, true, true, true);


            //Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
            if (Owner is GameNPC)
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)Owner).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(SpellHandler.Caster, 1);
            }
        }
        public override void OnStopEffect()
        {
            Owner.Disease(false);
            Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, EffectType);
            Owner.BuffBonusMultCategory1.Remove((int)eProperty.Strength, EffectType);

            // "You look healthy."
            // "{0} looks healthy again."
            OnEffectExpiresMsg(Owner, true, true, true);


            (SpellHandler as DiseaseSpellHandler).SendUpdates(this);
        }
    }
}