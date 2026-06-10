namespace DOL.GS
{
    /// <summary>
    /// Ability-Based Effect
    /// </summary>
    public class ECSGameAbilityEffect : ECSGameEffect, IPooledList<ECSGameAbilityEffect>
    {
        public override ushort TooltipId => Icon; // Workaround for abilities that don't define a tooltip ID. Needed for cancel via shift + right click to work. Can cause collisions.
        public override string Name => "Default Ability Name";

        public ECSGameAbilityEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override long GetNextTick()
        {
            return NextTick > 0 ? NextTick : base.GetNextTick();
        }
    }
}
