using System;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Quests;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Integration.Server
{
	/// <summary>
	/// Unit Test for GamePlayerTest.
	/// </summary>
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
			player.Level=5;
			player.AddToWorld();

			// Trainer for taks
			GameTrainer trainer = new GameTrainer();
			trainer.Name ="Tester";
			
			Console.WriteLine(player.Name);

			// player must have trainer selected when task given.
			player.TargetObject = trainer;

			// mob for task
			if (KillTask.BuildTask(player, trainer))
			{
				KillTask task = (KillTask)player.Task;

				ClassicAssert.IsNotNull(task);
				ClassicAssert.IsTrue(task.TaskActive);

				Console.WriteLine("Mob:" + task.MobName);
				Console.WriteLine("Item:" + task.ItemName);
				Console.WriteLine("" + task.Description);

				// Check Notify Event handling
				DbInventoryItem item = GameInventoryItem.Create(new DbItemTemplate());
				item.Name = task.ItemName;

				GameNPC mob = new GameNPC();
				mob.Name = task.MobName;
				mob.X = player.X;
				mob.Y = player.Y;
				mob.Z = player.Z;
				mob.Level = player.Level;
				mob.CurrentRegionID = player.CurrentRegionID;
				mob.AddToWorld();

				lock (mob.XpGainersLock)
				{ 
					// First we kill mob
					mob.XPGainers.Add(player, 1.0F);
				}

				// arificial pickup Item
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
				
				// Check item in Inventory
				if (player.Inventory.GetFirstItemByName(task.ItemName,eInventorySlot.FirstBackpack,eInventorySlot.LastBackpack) != null)
					ClassicAssert.Fail("Player didn't receive task item.");
				
				// Now give item tro trainer
				task.Notify(GamePlayerEvent.GiveItem,player,new GiveItemEventArgs(player,trainer,item));

				if (player.Task.TaskActive || player.Task==null)
					ClassicAssert.Fail("Task did not finished proper in Notify");
			}
		}
	}
}