namespace Core.GS
{
    public class EcsGameAbilityEffect : EcsGameEffect
    {
        public override string Name { get { return "Default Ability Name"; } }

        public EcsGameAbilityEffect(EcsGameEffectInitParams initParams) : base(initParams)
        {
            //EffectService.RequestStartEffect(this);
        }
    }
}