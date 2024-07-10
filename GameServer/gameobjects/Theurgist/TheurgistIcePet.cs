namespace DOL.GS
{
    public class TheurgistIcePet : TheurgistPet
    {
        public override double MaxHealthScalingFactor => 0.2575;

        public TheurgistIcePet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        // Ice pets stay permanently interrupted after their first one.
        public override bool IsBeingInterrupted => InterruptTime > 0;
        public override bool IsBeingInterruptedIgnoreSelfInterrupt => InterruptTime > 0;
    }
}
