using System;
using Core.Database.Tables;
using Core.GS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using NUnit.Framework;

namespace Core.Tests.Integration;

public class TestInventory : GameLivingInventory
{
}

[TestFixture]
public class GameLivingInventoryTest : ServerTests
{
	public GameLivingInventoryTest() : base()
	{
		
	}

	[Test]
	public void TestAddTemplate()
	{
		GameLivingInventory gameLivingInventory = new TestInventory();

		DbItemTemplate template = new DbItemTemplate();
		Random rand = new Random();
		template.Id_nb = "blankItem" + rand.Next().ToString();
		template.Name = "a blank item";
		template.MaxCount = 10;
		if (template == null)
			Console.WriteLine("template null");
		if (gameLivingInventory.AddTemplate(GameInventoryItem.Create(template), 7, EInventorySlot.RightHandWeapon, EInventorySlot.FourthQuiver))
			Console.WriteLine("addtemplate 7 blank item");
		else
			Console.WriteLine("can not add 7 blank item");
		Console.WriteLine("----PRINT AFTER FIRST ADD 7 TEMPLATE-----");
		PrintInventory(gameLivingInventory);

		if (gameLivingInventory.AddTemplate(GameInventoryItem.Create(template), 4, EInventorySlot.RightHandWeapon, EInventorySlot.FourthQuiver))
			Console.WriteLine("addtemplate 4 blank item");
		else
			Console.WriteLine("can not add 4 blank item");
		Console.WriteLine("----PRINT AFTER SECOND ADD 4 TEMPLATE-----");
		PrintInventory(gameLivingInventory);
		//here must have 10 item in a slot and 1 in another
		
	}
	
	public void PrintInventory(GameLivingInventory gameLivingInventory)
	{
		foreach(DbInventoryItem myitem in gameLivingInventory.AllItems)
		{
			Console.WriteLine("item ["+ myitem.SlotPosition +"] : " + myitem.Name + "(" +myitem.Count +")");
		}
	}
}