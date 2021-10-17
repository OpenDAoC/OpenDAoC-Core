using DOL.Language;

namespace DOL.GS
{
    public class StealthECSGameEffect : ECSGameEffect
    {
        public StealthECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override ushort Icon { get { return 0x193; } }
        public override string Name { get { return LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StealthEffect.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
        }

        public override void OnStopEffect()
        {
            if (Owner.HasAbility(Abilities.Camouflage))
            {
//                 IGameEffect camouflage = m_player.EffectList.GetOfType<CamouflageEffect>();
//                 if (camouflage != null)
//                     camouflage.Cancel(false);
            }
        }
    }
}