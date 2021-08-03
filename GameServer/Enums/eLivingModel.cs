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
    public enum eLivingModel : ushort
	{
		None = 0,
		#region AlbionClassModels
		BritonMale = 32,
		BritonFemale = 35,
		HighlanderMale = 39,
		HighlanderFemale = 43,
		SaracenMale = 48,
		SaracenFemale = 52,
		AvalonianMale = 61,
		AvalonianFemale = 65,
		InconnuMale = 716,
		InconnuFemale = 724,
		HalfOgreMale = 1008,
		HalfOgreFemale = 1020,
		MinotaurMaleAlb = 1395,
		#endregion
		#region MidgardClassModels
		TrollMale = 137,
		TrollFemale = 145,
		NorseMale = 503,
		NorseFemale = 507,
		KoboldMale = 169,
		KoboldFemale = 177,
		DwarfMale = 185,
		DwarfFemale = 193,
		ValkynMale = 773,
		ValkynFemale = 781,
		FrostalfMale = 1051,
		FrostalfFemale = 1063,
		MinotaurMaleMid = 1407,
		#endregion
		#region HiberniaClassModels
		FirbolgMale = 286,
		FirbolgFemale = 294,
		CeltMale = 302,
		CeltFemale = 310,
		LurikeenMale = 318,
		LurikeenFemale = 326,
		ElfMale = 334,
		ElfFemale = 342,
		SharMale = 1075,
		SharFemale = 1087,
		SylvanMale = 700,
		SylvanFemale = 708,
		MinotaurMaleHib = 1419,
		#endregion
		#region Hastener
		AlbionHastener = 244,
		MidgardHastener = 22,
		HiberniaHastener = 1910,
		#endregion Hastener
	}
}
