/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using DOL.GS;
using NUnit.Framework;

namespace DOL.Tests.Integration.Server
{
	/// <summary>
	/// Unit tests for the Zone Class
	/// </summary>
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
		testRegionData.Expansion = (int)eClientExpansion.None;
		testRegionData.Mobs = new DOL.Database.DbMob[]{
			new DOL.Database.DbMob() {},
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
			WorldMgr.RegisterZone(testZoneData, 0, 0, "TEST", 1, 1, 1, 1, (byte)eRealm.None);
	}

		[Test]
		public void GetRandomNPCTest()
		{		
			Zone zone = WorldMgr.GetZone(0);
			Assert.IsNotNull(zone);

			StartWatch();
			GameNPC npc = zone.GetRandomNPC(eRealm.None, 5, 7);
			// TODO(Blasnoc) the two following nullchecks always skip because there are no mobs in the db.
			// 	this test should be enhanced with actual mobs.
			if (npc != null)
			{
				Console.WriteLine($"Found NPC from Realm None in {zone.ZoneRegion.Description}/{zone.Description}:{npc.Name} level:{npc.Level}");

				Assert.GreaterOrEqual(npc.Level, 5, "NPC Level out of defined range");
				Assert.LessOrEqual(npc.Level, 7, "NPC Level out of defined range");
				Assert.AreEqual(eRealm.None, npc.Realm, "NPC wrong realm");
			}
			else
			{
				Console.WriteLine("nothing found in " + zone.ZoneRegion.Description + "/" + zone.Description);
			}
			StopWatch();

			StartWatch();
			npc = zone.GetRandomNPC(eRealm.Albion);
			if (npc != null)
			{
				Console.WriteLine("Found Albion NPC in " + zone.ZoneRegion.Description + "/" + zone.Description + ":" + npc.Name);

				if (npc.Realm != eRealm.Albion)
					Assert.Fail("NPC wrong Realm");
			}
			else
			{
				Console.WriteLine("nothing found in " + zone.ZoneRegion.Description + "/" + zone.Description);
			}
			StopWatch();
		}
	}
}
