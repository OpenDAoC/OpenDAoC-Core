using System;
using Core.Database.Tables;
using Core.GS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Quests;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class KillTaskTest : ServerTests
{
	public KillTaskTest()
	{
	}

	[Test, Explicit]
	public void CreateKillTask()
	{
		GamePlayer player = CreateMockGamePlayer();
		player.Level = 5;
		player.AddToWorld();

		// Trainer for taks
		GameTrainer trainer = new GameTrainer();
		trainer.Name = "Tester";

		Console.WriteLine(player.Name);

		// player must have trainer selected when task given.
		player.TargetObject = trainer;

		// mob for task
		if (KillTask.BuildTask(player, trainer))
		{
			KillTask task = (KillTask)player.Task;

			Assert.IsNotNull(task);
			Assert.IsTrue(task.TaskActive);

			Console.WriteLine("Mob:" + task.MobName);
			Console.WriteLine("Item:" + task.ItemName);
			Console.WriteLine("" + task.Description);

			// Check Notify Event handling
			DbInventoryItem item = GameInventoryItem.Create(new DbItemTemplate());
			item.Name = task.ItemName;

			GameNpc mob = new GameNpc();
			mob.Name = task.MobName;
			mob.X = player.X;
			mob.Y = player.Y;
			mob.Z = player.Z;
			mob.Level = player.Level;
			mob.CurrentRegionID = player.CurrentRegionID;
			mob.AddToWorld();

			lock (mob.XPGainers.SyncRoot)
			{
				// First we kill mob
				mob.XPGainers.Add(player, 1.0F);
			}

			// arificial pickup Item
			player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, item);

			// Check item in Inventory
			if (player.Inventory.GetFirstItemByName(task.ItemName, EInventorySlot.FirstBackpack,
				    EInventorySlot.LastBackpack) != null)
				Assert.Fail("Player didn't receive task item.");

			// Now give item tro trainer
			task.Notify(GamePlayerEvent.GiveItem, player, new GiveItemEventArgs(player, trainer, item));

			if (player.Task.TaskActive || player.Task == null)
				Assert.Fail("Task did not finished proper in Notify");
		}
	}
}