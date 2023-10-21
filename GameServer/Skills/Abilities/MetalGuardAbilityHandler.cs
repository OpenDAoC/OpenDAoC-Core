namespace Core.GS.SkillHandler
{
    [SkillHandler(Abilities.MetalGuard)]
    public class MetalGuardAbilityHandler : SpellCastingAbilityHandler
    {
		public override long Preconditions
		{
			get
			{
				return DEAD | SITTING | MEZZED | STUNNED | NOTINGROUP;
			}
		}
 		public override int SpellID
		{
			get
			{
				return 14375;
			}
		}     
    }
}
