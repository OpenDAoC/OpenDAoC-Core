namespace DOL.GS
{
	public class TheurgistIcePet : TheurgistPet
	{
		public TheurgistIcePet(INpcTemplate npcTemplate) : base(npcTemplate) { }

		// Ice pets stay permanently interrupted after their first one.
		// This will break if these values are reset for any reason.
		public override bool IsBeingInterrupted => InterruptTime > 0 || SelfInterruptTime > 0;
		public override bool IsBeingInterruptedIgnoreSelfInterrupt => InterruptTime > 0;

		// They are however able to cast after hitting someone in melee but not getting interrupted.
		// So it's important that it doesn't add an interrupt timer on itself.
		// TODO: Maybe find a way to differentiate both.
		public override bool StartInterruptTimerOnItselfOnMeleeAttack()
		{
			return false;
		}
	}
}
