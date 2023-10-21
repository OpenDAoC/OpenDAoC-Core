namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Sprint)]
	public class SprintAbilityHandler : IAbilityActionHandler
	{
		public void Execute(Ability ab, GamePlayer player)
		{
			player.Sprint(!player.IsSprinting);
		}
	}
}
