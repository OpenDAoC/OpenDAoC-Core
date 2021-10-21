

namespace DOL.GS
{
    /// <summary>
    /// Ability-Based Effect
    /// </summary>
    public class ECSGameAbilityEffect : ECSGameEffect
    {
        public override string Name { get { return "Default Ability Name"; } }

        public ECSGameAbilityEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EntityManager.AddEffect(this);
        }
    }
}