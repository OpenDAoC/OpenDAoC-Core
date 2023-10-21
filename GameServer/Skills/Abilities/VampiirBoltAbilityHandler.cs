namespace Core.GS.Skills;

[SkillHandler(AbilityConstants.VampiirBolt)]
public class VampiirBoltAbilityHandler : SpellCastingAbilityHandler
{
	public override long Preconditions
	{
		get
		{
			return DEAD | SITTING | MEZZED | STUNNED | TARGET;
		}
	}

	public override int SpellID
	{
		get
		{
			return 13200 + m_ability.Level;
		}
	}
}