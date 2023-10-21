using Core.Database;
using Core.Database.Tables;

namespace Core.GS
{
	/// <summary>
	/// Adds money chests to a Mobs droppable loot based on a chance set in server properties
	/// </summary>
	public class LootGeneratorChest : LootGeneratorBase
	{	
		int SMALLCHEST_CHANCE = ServerProperties.Properties.BASE_SMALLCHEST_CHANCE;
		int LARGECHEST_CHANCE = ServerProperties.Properties.BASE_LARGECHEST_CHANCE;
		public override LootList GenerateLoot(GameNpc mob, GameObject killer)
		{
			LootList loot = base.GenerateLoot(mob, killer);
			int small = SMALLCHEST_CHANCE;
			int large = LARGECHEST_CHANCE;
			if (Util.Chance(small))
			{
				int lvl = mob.Level + 1;
				if (lvl < 1) lvl = 1;
				int minLoot = ServerProperties.Properties.SMALLCHEST_MULTIPLIER * (lvl * lvl); 
				long moneyCount = minLoot + Util.Random(minLoot >> 1);
				moneyCount = (long)((double)moneyCount * ServerProperties.Properties.MONEY_DROP);
				DbItemTemplate money = new DbItemTemplate();
				money.Model = 488;
				money.Name = "small chest";
				money.Level = 0;
				money.Price = moneyCount;
				loot.AddFixed(money, 1);
			}
			if (Util.Chance(large))
			{
				int lvl = mob.Level + 1;
				if (lvl < 1) lvl = 1;
				int minLoot = ServerProperties.Properties.LARGECHEST_MULTIPLIER * (lvl * lvl); 
				long moneyCount = minLoot + Util.Random(minLoot >> 1);
				moneyCount = (long)((double)moneyCount * ServerProperties.Properties.MONEY_DROP);
				DbItemTemplate money = new DbItemTemplate();
				money.Model = 488;
				money.Name = "large chest";
				money.Level = 0;
				money.Price = moneyCount;
				loot.AddFixed(money, 1);
			}
			return loot;
		}
	}
}