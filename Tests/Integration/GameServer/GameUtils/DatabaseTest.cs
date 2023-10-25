using System;
using Core.Database.Tables;
using Core.GS;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class DatabaseTest : ServerTests
{
	public DatabaseTest()
	{
	}

	[Test]
	public void TestSelect()
	{
		Console.WriteLine("TestSelect();");

		var obs = GameServer.Database.SelectAllObjects<DbItemTemplate>();
		Console.WriteLine("ItemTemplates Type="+obs.GetType());

		var items = GameServer.Database.SelectAllObjects<DbMerchantItem>();
		Console.WriteLine("MerchantItems Type="+items.GetType());
		
	}			
}