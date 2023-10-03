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
    /// This enumeration holds all equipment
    /// items that can be used by a player
    /// </summary>
    public enum eEquipmentItems : byte
	{
		HORSE = 0x09,
		RIGHT_HAND = 0x0A,
		LEFT_HAND = 0x0B,
		TWO_HANDED = 0x0C,
		RANGED = 0x0D,
		HEAD = 0x15,
		HAND = 0x16,
		FEET = 0x17,
		JEWEL = 0x18,
		TORSO = 0x19,
		CLOAK = 0x1A,
		LEGS = 0x1B,
		ARMS = 0x1C,
		NECK = 0x1D,
		WAIST = 0x20,
		L_BRACER = 0x21,
		R_BRACER = 0x22,
		L_RING = 0x23,
		R_RING = 0x24,
		MYTHICAL = 0x25
	};
}
