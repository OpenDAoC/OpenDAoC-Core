using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.Skills;

[SkillHandler(AbilityConstants.ScarsOfBattle)]
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