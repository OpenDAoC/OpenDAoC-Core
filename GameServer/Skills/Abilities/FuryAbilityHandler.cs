namespace DOL.GS.SkillHandler
{
    [SkillHandler(Abilities.Fury)]
    public class FuryAbilityHandler : SpellCastingAbilityHandler
    {
		public override long Preconditions
		{
			get
			{
				return DEAD | SITTING | MEZZED | STUNNED;
			}
		}
		public override int SpellID
		{
			get
			{
				return 14374;
			}
		}     
    }
}
