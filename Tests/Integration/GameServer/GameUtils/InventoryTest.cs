using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.Database;
using Core.GS.Enums;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class InventoryTest : ServerTests
{
	static GamePlayer player;
	static DbItemTemplate itemt;
	static DbItemUnique itemu;
	
	public InventoryTest() {}
	
	// the following is not in the constructor because TestFixtureSetup
	// is not initialized at this time, thus we can't connect to server
	public void InventoryTestCreation()
	{
		player = CreateMockGamePlayer();
		Assert.IsNotNull(player, "Player is null !");
		itemt = CoreDb<DbItemTemplate>.SelectObject(DB.Column("Id_nb").IsEqualTo("championDocharWardenBlade"));
		Assert.IsNotNull(itemt, "ItemTemplate is null !");
		itemu = new DbItemUnique();
		itemu.Id_nb = "tunik"+DateTime.Now.Ticks;
		GameServer.Database.AddObject(itemu);
		Assert.IsNotNull(itemu, "ItemUnique is created !");
		_ = CoreDb<DbItemTemplate>.SelectObject(DB.Column("id_nb").IsEqualTo("traitors_dagger_hib"));
	}

	/* Tests for items - 1/ IT 2/ IU 3/ Ghost
	 * 
	 * 1) create an invitem
	 * 2) check inventory table
	 * 3) delete this invitem
	 * 4) check inventory table
	 * 5) check IT / IU tables
	 * 
	 */
	[Test, Explicit]
	public void InventoryFromIT()
	{
		InventoryTestCreation();
		Console.WriteLine("Creation of Ghost Inventory entry based on ItemTemplate");

		DbInventoryItem ii = GameInventoryItem.Create(itemt);
		player.Inventory.AddItem(EInventorySlot.FirstBackpack, ii);
		Assert.IsNotNull(ii, "ii-t #1 : " + ii.Template.Id_nb + " created & added to " + ii.OwnerID);
		var iicheck = GameServer.Database.FindObjectByKey<DbInventoryItem>(ii.ObjectId);
		Assert.IsNotNull(iicheck, "ii-t #2 : saved in db " + ii.Template.Id_nb + " to " + ii.OwnerID);
		GameServer.Database.DeleteObject(ii);
		iicheck = GameServer.Database.FindObjectByKey<DbInventoryItem>(ii.ObjectId);
		Assert.IsNull(iicheck, "ii-t #3 : deleted from db " + ii.Template.Id_nb + " to " + ii.OwnerID);
		var itcheck = GameServer.Database.FindObjectByKey<DbItemTemplate>(itemt.Id_nb);
		Assert.IsNotNull(itcheck, "ii-t #4 : not deleted from db " + itemt.Id_nb);
	}
	
	[Test, Explicit]
	public void InventoryFromIU()
	{
		InventoryTestCreation();
		Console.WriteLine("Creation of Inventory entry based on ItemUnique");

		DbInventoryItem ii = GameInventoryItem.Create(itemu);
		player.Inventory.AddItem(EInventorySlot.FirstBackpack, ii);
		Assert.IsNotNull(ii, "ii-u #1 : " + ii.Template.Id_nb + " created & added to " + ii.OwnerID);
		var iicheck = GameServer.Database.FindObjectByKey<DbInventoryItem>(ii.ObjectId);
		Assert.IsNotNull(iicheck, "ii-u #2 : saved in db " + ii.Template.Id_nb + " to " + ii.OwnerID);
		var iucheck = GameServer.Database.FindObjectByKey<DbItemUnique>(itemu.Id_nb);
		Assert.IsNotNull(iicheck, "ii-u #3 : saved to db " + itemu.Id_nb);
		GameServer.Database.DeleteObject(ii);
		iicheck = GameServer.Database.FindObjectByKey<DbInventoryItem>(ii.ObjectId);
		Assert.IsNull(iicheck, "ii-u #4 : deleted from db " + ii.Template.Id_nb + " to " + ii.OwnerID);
		iucheck = GameServer.Database.FindObjectByKey<DbItemUnique>(itemu.Id_nb);
		Assert.IsNull(iucheck, "ii-t #5 : deleted from db " + itemu.Id_nb);
		
	}
	
	[Test, Explicit]
	public void InventoryFromNull()
	{
		InventoryTestCreation();
		Console.WriteLine("Creation of Ghost Inventory entry based on ItemTemplate");

		DbInventoryItem ii = new DbInventoryItem();
		player.Inventory.AddItem(EInventorySlot.FirstBackpack, ii);
		Assert.IsNotNull(ii, "ii-g #1 : " + ii.Template.Id_nb + " created & added to " + ii.OwnerID);
		var iicheck = GameServer.Database.FindObjectByKey<DbInventoryItem>(ii.ObjectId);
		Assert.IsNull(iicheck, "ii-g #2 : not saved in db " + ii.Template.Id_nb + " to " + ii.OwnerID);
	}
}