namespace DOL.GS
{
    /// <summary>
    /// Ability-Based Effect
    /// </summary>
    public class ECSGameAbilityEffect : ECSGameEffect, IPooledList<ECSGameAbilityEffect>
    {
        public override string Name { get { return "Default Ability Name"; } }

        public ECSGameAbilityEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }
    }
}