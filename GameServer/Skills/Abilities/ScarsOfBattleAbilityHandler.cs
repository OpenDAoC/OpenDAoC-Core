using DOL.Database;

namespace DOL.GS.SkillHandler
{
	[SkillHandler(Abilities.ScarsOfBattle)]
	public class ScarsOfBattleAbilityHandler : StatChangingAbility
	{
		public ScarsOfBattleAbilityHandler(DbAbility dba, int level)
			: base(dba, 1, EProperty.MaxHealth)
		{
		}
		public override int GetAmountForLevel(int level)
		{
			return 10;
		}
	}
}
