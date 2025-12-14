using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Disease)]
    public class DiseaseSpellHandler : SpellHandler
    {
        private bool _critical;

        public override string ShortDescription => $"Inflicts a wasting disease on the target that slows target by 15%, reduces its strength by 7.5% and inhibits healing by 50%";

        public DiseaseSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, _critical, static (in i, critical) => new DiseaseECSGameEffect(i, critical));
        }

        protected override double GetDebuffEffectivenessCriticalModifier()
        {
            double effectiveness = base.GetDebuffEffectivenessCriticalModifier();

            // If the effectiveness is greater than 1.0, register the effect as critical.
            // This will be passed to the effect so that the snare component can be doubled.
            if (effectiveness > 1.0)
                _critical = true;

            return effectiveness;
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            base.ApplyEffectOnTarget(target);
        }

        public override void OnEffectStart(GameSpellEffect effect) { }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            return 0;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = base.CalculateEffectDuration(target);
            duration -= duration * target.GetResist(Spell.DamageType) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = Spell.Duration * 4;

            return (int) duration;
        }

        public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            if (Spell.Pulse != 0 || Spell.Concentration != 0 || e.RemainingTime < 1)
                return null;

            DbPlayerXEffect eff = new()
            {
                Var1 = Spell.ID,
                Duration = e.RemainingTime,
                IsHandler = true,
                SpellLine = SpellLine.KeyName
            };

            return eff;
        }

        public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
        {
            effect.Owner.Disease(true);
            effect.Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, effect, 1.0 - 0.15);
            effect.Owner.BuffBonusMultCategory1.Set((int) eProperty.Strength, effect, 1.0 - 0.075);
        }

        public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
        {
            return OnEffectExpires(effect, noMessages);
        }
    }
}
