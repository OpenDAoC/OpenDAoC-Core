namespace DOL.GS
{
    public class ResurrectionIllnessECSGameEffect : ECSGameSpellEffect
    {
        public ResurrectionIllnessECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            if (Owner is not GamePlayer player)
                return;

            // Overwrite Duration. Higher level rez spells reduce duration of rez sick.
            if (player.TempProperties.GetAllProperties().Contains(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS))
            {
                double rezSickEffectiveness = player.TempProperties.GetProperty<double>(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS);
                player.TempProperties.RemoveProperty(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS);
                Duration = (int) (initParams.Duration * rezSickEffectiveness);
            }

            if (player.GetModified(eProperty.ResIllnessReduction) > 0)
                Duration = initParams.Duration * (100 - player.GetModified(eProperty.ResIllnessReduction)) / 100;
        }

        public override void OnStartEffect()
        {
            if (Owner is GamePlayer player)
            {
                player.Effectiveness -= SpellHandler.Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
        }

        public override void OnStopEffect()
        {
            if (Owner is GamePlayer player)
            {
                player.Effectiveness += SpellHandler.Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
        }
    }
}
