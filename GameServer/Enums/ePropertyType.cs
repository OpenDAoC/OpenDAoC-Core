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

namespace DOL.GS
{
    /// <summary>
    /// All property types for check using SkillBase.CheckPropertyType. Must be unique bits set.
    /// </summary>
    [Flags]
	public enum ePropertyType : ushort
	{
		Focus = 1,
		Resist = 1 << 1,
		Skill = 1 << 2,
		SkillMeleeWeapon = 1 << 3,
		SkillMagical = 1 << 4,
		SkillDualWield = 1 << 5,
		SkillArchery = 1 << 6,
		ResistMagical = 1 << 7,
		Albion = 1 << 8,
		Midgard = 1 << 9,
		Hibernia = 1 << 10,
		Common = 1 << 11,
		CapIncrease = 1 << 12,
	}
}
