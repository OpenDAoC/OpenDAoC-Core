namespace Core.GS.Skills;

[SkillHandler(AbilityConstants.Sprint)]
public class SprintAbilityHandler : IAbilityActionHandler
{
	public void Execute(Ability ab, GamePlayer player)
	{
		player.Sprint(!player.IsSprinting);
	}
}