namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.StyleBleeding)]
    public class StyleBleeding : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public StyleBleeding(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new BleedECSEffect(initParams);
        }

        protected override double CalculateDamageEffectiveness()
        {
            return CasterEffectiveness;
        }

        public override AttackData CalculateDamageToTarget(GameLiving target)
        {
            AttackData ad = new()
            {
                Attacker = Caster,
                Target = target,
                AttackType = AttackData.eAttackType.Spell,
                DamageType = Spell.DamageType,
                AttackResult = eAttackResult.HitUnstyled,
                SpellHandler = this,
                CausesCombat = false
            };

            // `Modifier` and `Damage` have to be set by the caller (normally a `BleedECSEffect`).
            return ad;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            return Spell.Duration;
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
    }
}
