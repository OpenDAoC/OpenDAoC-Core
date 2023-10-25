using System;
using Core.GS;
using Core.GS.Events;
using Core.GS.World;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class RegionTest : ServerTests
{		
	public static bool notified = false;

	public RegionTest()
	{
	}		

	[Test, Explicit]
	public void AddObject()
	{
		Region region = WorldMgr.GetRegion(1);
		GameObject obj = new GameNpc();
		obj.Name="TestObject";
		obj.X = 400000;
		obj.Y = 200000;
		obj.Z = 2000;
		obj.CurrentRegion = region;

		obj.AddToWorld();

		if (obj.ObjectID<0)
			Assert.Fail("Failed to add object to Region. ObjectId < 0");

		Assert.AreEqual(region.GetObject((ushort)obj.ObjectID),obj);
	}


	[Test, Explicit]
	public void AddArea()
	{
		Region region = WorldMgr.GetRegion(1);
		IArea insertArea = region.AddArea(new Area.Circle(null,1000,1000,0,500));

		Assert.IsNotNull(insertArea);

		var areas = region.GetAreasOfSpot(501,1000,0);			
		Assert.IsTrue(areas.Count>0);

		bool found = false;
		foreach( IArea ar in areas)
		{
			if (ar == insertArea) 
			{
				found = true;	
				break;
			}
		}
		Assert.IsTrue(found);

		//
		areas = region.GetAreasOfSpot(1499,1000,2000);			
		Assert.IsTrue(areas.Count>0);

		found = false;
		foreach( IArea ar in areas)
		{
			if (ar == insertArea) 
			{
				found = true;	
				break;
			}
		}
		Assert.IsTrue(found);


		//Notify test
		notified=false;

		GamePlayer player = CreateMockGamePlayer();
		
		insertArea.RegisterPlayerEnter(new CoreEventHandler(NotifyTest));
		insertArea.OnPlayerEnter(player);

		Assert.IsTrue(notified);

		region.RemoveArea(insertArea);

		areas = region.GetAreasOfSpot(1499,1000,2000);
		Assert.IsTrue(areas.Count==0);

	}

	public static void NotifyTest(CoreEvent e, object sender, EventArgs args)
	{
		Console.WriteLine("notified");
		notified = true;
	}

	[Test]
	public void RemoveObject()
	{			
	}
}