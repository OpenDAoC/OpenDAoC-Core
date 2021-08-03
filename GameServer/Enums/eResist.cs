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
    /// resists
    /// </summary>
    public enum eResist : byte
	{
		Natural = eProperty.Resist_Natural,
		Crush = eProperty.Resist_Crush,
		Slash = eProperty.Resist_Slash,
		Thrust = eProperty.Resist_Thrust,
		Body = eProperty.Resist_Body,
		Cold = eProperty.Resist_Cold,
		Energy = eProperty.Resist_Energy,
		Heat = eProperty.Resist_Heat,
		Matter = eProperty.Resist_Matter,
		Spirit = eProperty.Resist_Spirit
	}
}
