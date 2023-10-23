using System;
using Core.Database.Tables;
using Core.GS;
using Core.GS.GameUtils;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class LootManagerTest : ServerTests
{
	public LootManagerTest()
	{
	}

	[Test] 
	public void TestLootGenerator()
	{						
		GameNpc mob = new GameNpc();
		mob.Level = 6;
		mob.Name="impling";

		for (int i=0;i< 15; i++) 
		{
			Console.WriteLine("Loot "+i);
			DbItemTemplate[] loot = LootMgr.GetLoot(mob, null);
			foreach (DbItemTemplate item in loot)
			{
				Console.WriteLine(mob.Name+" drops "+item.Name);
			}	
		}
		
		Console.WriteLine("Drops finished");
	}
}