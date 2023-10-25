namespace Core.GS.Skills;

[SkillHandler(AbilityConstants.TauntingShout)]
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