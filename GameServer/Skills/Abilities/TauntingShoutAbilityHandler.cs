namespace Core.GS.SkillHandler
{
    [SkillHandler(Abilities.TauntingShout)]
    public class TauntingShoutAbilityHandler : SpellCastingAbilityHandler
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
				return 14377;
			}
		}     
    }
}
