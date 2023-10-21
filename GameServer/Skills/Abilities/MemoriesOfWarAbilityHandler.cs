using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.SkillHandler
{
	//Memories of War: Upon reaching level 41, the Hero, Warrior and Armsman will begin to gain more magic resistance
	//(spell damage reduction only) as they progress towards level 50. At each level beyond 41 they gain 2% extra
	//resistance per level. At level 50, they will have the full 20% benefit.
	[SkillHandler(Abilities.MemoriesOfWar)]
	public class MemoriesOfWarAbilityHandler : StatChangingAbility
	{
		public MemoriesOfWarAbilityHandler(DbAbility dba, int level)
			: base(dba, level, new EProperty[] {
							EProperty.Resist_Body,
							EProperty.Resist_Cold,
							EProperty.Resist_Energy,
							EProperty.Resist_Heat,
							EProperty.Resist_Matter,
							EProperty.Resist_Spirit, })
		{
		}

		// Caps at level 10.
		public override int GetAmountForLevel(int level)
		{
			return Math.Min(10, level) * 2;
		}
	}
}
