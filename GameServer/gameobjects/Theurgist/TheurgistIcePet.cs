namespace DOL.GS
{
    public class TheurgistIcePet : TheurgistPet
    {
        public override double MaxHealthScalingFactor => 0.2575;

        public TheurgistIcePet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}
