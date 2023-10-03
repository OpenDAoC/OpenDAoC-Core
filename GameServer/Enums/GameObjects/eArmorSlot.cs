﻿/*
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
    /// This enumeration holds all slots that can wear attackable armor
    /// </summary>
    public enum eArmorSlot : int
	{
		NOTSET = 0x00,
		HEAD = eInventorySlot.HeadArmor,
		HAND = eInventorySlot.HandsArmor,
		FEET = eInventorySlot.FeetArmor,
		TORSO = eInventorySlot.TorsoArmor,
		LEGS = eInventorySlot.LegsArmor,
		ARMS = eInventorySlot.ArmsArmor,
	};
}
