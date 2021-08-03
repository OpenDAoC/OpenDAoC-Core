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
    /// Holds all character classes
    /// </summary>
    public enum eCharacterClass : byte
	{
		Unknown = 0,

		//base classes
		Acolyte = 16,
		AlbionRogue = 17,
		Disciple = 20,
		Elementalist = 15,
		Fighter = 14,
		Forester = 57,
		Guardian = 52,
		Mage = 18,
		Magician = 51,
		MidgardRogue = 38,
		Mystic = 36,
		Naturalist = 53,
		Seer = 37,
		Stalker = 54,
		Viking = 35,

		//alb classes
		Armsman = 2,
		Cabalist = 13,
		Cleric = 6,
		Friar = 10,
		Heretic = 33,
		Infiltrator = 9,
		Mercenary = 11,
		Minstrel = 4,
		Necromancer = 12,
		Paladin = 1,
		Reaver = 19,
		Scout = 3,
		Sorcerer = 8,
		Theurgist = 5,
		Wizard = 7,
		MaulerAlb = 60,

		//mid classes
		Berserker = 31,
		Bonedancer = 30,
		Healer = 26,
		Hunter = 25,
		Runemaster = 29,
		Savage = 32,
		Shadowblade = 23,
		Shaman = 28,
		Skald = 24,
		Spiritmaster = 27,
		Thane = 21,
		Valkyrie = 34,
		Warlock = 59,
		Warrior = 22,
		MaulerMid = 61,

		//hib classes
		Animist = 55,
		Bainshee = 39,
		Bard = 48,
		Blademaster = 43,
		Champion = 45,
		Druid = 47,
		Eldritch = 40,
		Enchanter = 41,
		Hero = 44,
		Mentalist = 42,
		Nightshade = 49,
		Ranger = 50,
		Valewalker = 56,
		Vampiir = 58,
		Warden = 46,
		MaulerHib = 62,
	}
}
