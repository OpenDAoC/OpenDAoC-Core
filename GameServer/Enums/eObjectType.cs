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

namespace DOL.GS
{
    /// <summary>
    /// Object type sets the type of object, for example sword or shield
    /// </summary>
    public enum eObjectType : byte
	{
		GenericItem = 0,
		GenericWeapon = 1,

		//Albion weapons
		_FirstWeapon = 2,
		CrushingWeapon = 2,
		SlashingWeapon = 3,
		ThrustWeapon = 4,
		Fired = 5,
		TwoHandedWeapon = 6,
		PolearmWeapon = 7,
		Staff = 8,
		Longbow = 9,
		Crossbow = 10,
		Flexible = 24,

		//Midgard weapons
		Sword = 11,
		Hammer = 12,
		Axe = 13,
		Spear = 14,
		CompositeBow = 15,
		Thrown = 16,
		LeftAxe = 17,
		HandToHand = 25,

		//Hibernia weapons
		RecurvedBow = 18,
		Blades = 19,
		Blunt = 20,
		Piercing = 21,
		LargeWeapons = 22,
		CelticSpear = 23,
		Scythe = 26,

		//Mauler weapons
		FistWraps = 27,
		MaulerStaff = 28,
		_LastWeapon = 28,

		//Armor
		_FirstArmor = 31,
		GenericArmor = 31,
		Cloth = 32,
		Leather = 33,
		Studded = 34,
		Chain = 35,
		Plate = 36,
		Reinforced = 37,
		Scale = 38,
		_LastArmor = 38,

		//Misc
		Magical = 41,
		Shield = 42,
		Arrow = 43,
		Bolt = 44,
		Instrument = 45,
		Poison = 46,
		AlchemyTincture = 47,
		SpellcraftGem = 48,

		//housing
		_FirstHouse = 49,
		GardenObject = 49,
		HouseWallObject = 50,
		HouseFloorObject = 51,
		HouseCarpetFirst = 52,
		HouseNPC = 53,
		HouseVault = 54,
		HouseInteriorObject = 55, //Lathe, forge, alchemy table
		HouseTentColor = 56,
		HouseExteriorBanner = 57,
		HouseExteriorShield = 58,
		HouseRoofMaterial = 59,
		HouseWallMaterial = 60,
		HouseDoorMaterial = 61,
		HousePorchMaterial = 62,
		HouseWoodMaterial = 63,
		HouseShutterMaterial = 64,
		HouseInteriorBanner = 66,
		HouseInteriorShield = 67,
		HouseBindstone = 68,
		HouseCarpetSecond = 69,
		HouseCarpetThird = 70,
		HouseCarpetFourth = 71,
		_LastHouse = 71,

		//siege weapons
		SiegeBalista = 80, // need log
		SiegeCatapult = 81, // need log
		SiegeCauldron = 82, // need log
		SiegeRam = 83, // need log
		SiegeTrebuchet = 84, // need log
	}
}
