namespace DOL.GS
{
    public class TheurgistEarthPet : TheurgistPet
    {
        public override double MaxHealthScalingFactor => 0.18;

        public TheurgistEarthPet(INpcTemplate npcTemplate) : base(npcTemplate)
        {
            DamageFactor = 1.15;
        }
    }
}
