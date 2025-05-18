using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_ShadowRunECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_ShadowRunECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.ShadowRun;
            Start();
        }

        public override void OnStartEffect()
        {
            base.OnStartEffect();
            if (Owner is GamePlayer p)
            {
                p.Out.SendUpdateMaxSpeed();
            }
        }

        public override void OnStopEffect()
        {
            base.OnStopEffect();
            if (Owner is GamePlayer p)
            {
                p.Out.SendUpdateMaxSpeed();
            }
        }

        public override ushort Icon { get { return 4278; } }
        public override string Name { get { return "Shadow Run"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
