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
            double baseSpeedDebuff = 0.15;
            double baseStrDebuff = 0.075;

            /*
            if (SpellHandler.Caster.HasAbilityType(typeof(AtlasOF_WildArcanaAbility)))
            {
                if (Util.Chance(SpellHandler.Caster.SpellCriticalChance))
                {
                    double modSpeed = baseSpeedDebuff * 2;
                    double modStr = baseStrDebuff * (1 + Util.Random(1, 10) * .1);
                    if(Caster is GamePlayer c) c.Out.SendMessage($"Your {SpellHandler.Spell.Name} critically debuffs the enemy for {Math.Round(modStr - baseStrDebuff) * 100}% additional effect!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    baseSpeedDebuff = modSpeed;
                    baseStrDebuff = modStr;
                }
            }*/

            Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, EffectType, 1.0 - baseSpeedDebuff);
            Owner.BuffBonusMultCategory1.Set((int)eProperty.Strength, EffectType, 1.0 - baseStrDebuff);
            Owner.OnMaxSpeedChange();

            // "You are diseased!"
            // "{0} is diseased!"
            OnEffectStartsMsg(Owner, true, true, true);
            //Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);

            if (Owner is GameNPC npcOwner)
            {
                IOldAggressiveBrain aggroBrain = npcOwner.Brain as IOldAggressiveBrain;
                aggroBrain?.AddToAggroList(SpellHandler.Caster, 1);
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
            Owner.OnMaxSpeedChange();
        }
    }
}
