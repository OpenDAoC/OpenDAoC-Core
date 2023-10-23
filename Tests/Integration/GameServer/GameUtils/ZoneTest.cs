using System;
using Core.Database.Tables;
using Core.GS;
using Core.GS.Enums;
using Core.GS.World;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class ZoneTest : ServerTests
{
	public ZoneTest()
	{
	}

	// [Test]
	public void GetZCoordinateTest()
	{
	}

[SetUp]
public void InitGetRandomNpcTest() 
{
	var testRegionData = new RegionData();
	testRegionData.Id = 0;
	testRegionData.Ip = "127.0.0.1";
	testRegionData.IsFrontier = false;
	testRegionData.Name = "TEST";
	testRegionData.Description = "TEST";
	testRegionData.Port = 42069;
	testRegionData.WaterLevel = 0;
	testRegionData.DivingEnabled = false;
	testRegionData.HousingEnabled = false;
	testRegionData.Expansion = (int)EClientExpansion.None;
	testRegionData.Mobs = new DbMob[]{
		new DbMob() {},
	};

	var testZoneData = new ZoneData(){
		ZoneID = 0,
		RegionID = testRegionData.Id,
		OffX = 1,
		OffY = 1,
		Height = 1,
		Width = 1,
		Description = "TEST",
		DivingFlag = 0,
		WaterLevel = testRegionData.WaterLevel,
		IsLava = false,
	};

	// TODO(Blasnoc) these nullchecks are temporary fixes until I figure out the disparities between
	// the appveyor env and local.
	if(WorldMgr.GetRegion(testRegionData.Id) == null)
		WorldMgr.RegisterRegion(testRegionData);
	if(WorldMgr.GetZone(testZoneData.ZoneID) == null)
		WorldMgr.RegisterZone(testZoneData, 0, 0, "TEST", 1, 1, 1, 1, (byte)ERealm.None);
}

	[Test]
	public void GetRandomNPCTest()
	{		
		Zone zone = WorldMgr.GetZone(0);
		Assert.IsNotNull(zone);

		StartWatch();
		GameNpc npc = zone.GetRandomNPC(ERealm.None, 5, 7);
		// TODO(Blasnoc) the two following nullchecks always skip because there are no mobs in the db.
		// 	this test should be enhanced with actual mobs.
		if (npc != null)
		{
			Console.WriteLine($"Found NPC from Realm None in {zone.ZoneRegion.Description}/{zone.Description}:{npc.Name} level:{npc.Level}");

			Assert.GreaterOrEqual(npc.Level, 5, "NPC Level out of defined range");
			Assert.LessOrEqual(npc.Level, 7, "NPC Level out of defined range");
			Assert.AreEqual(ERealm.None, npc.Realm, "NPC wrong realm");
		}
		else
		{
			Console.WriteLine("nothing found in " + zone.ZoneRegion.Description + "/" + zone.Description);
		}
		StopWatch();

		StartWatch();
		npc = zone.GetRandomNPC(ERealm.Albion);
		if (npc != null)
		{
			Console.WriteLine("Found Albion NPC in " + zone.ZoneRegion.Description + "/" + zone.Description + ":" + npc.Name);

			if (npc.Realm != ERealm.Albion)
				Assert.Fail("NPC wrong Realm");
		}
		else
		{
			Console.WriteLine("nothing found in " + zone.ZoneRegion.Description + "/" + zone.Description);
		}
		StopWatch();
	}
}