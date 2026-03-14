namespace DOL.GS
{
    public class TheurgistAirPet : TheurgistPet
    {
        public override double MaxHealthScalingFactor => 0.4115;

        public TheurgistAirPet(INpcTemplate npcTemplate) : base(npcTemplate)
        {
            DamageFactor = 0.75;
        }

        // Theurgist air pets have only one spell and use their own logic.
        public override bool IsInstantHarmfulSpellCastingLocked => false;

        public override void ApplyInstantHarmfulSpellDelay()
        {
            return;
        }
    }
}
