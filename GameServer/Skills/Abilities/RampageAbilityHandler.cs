namespace DOL.GS.SkillHandler
{
    [SkillHandler(Abilities.Rampage)]
    public class RampageAbilityHandler : SpellCastingAbilityHandler
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
				return 14373;
			}
		}      
    }
}
