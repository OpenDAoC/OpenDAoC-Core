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

using System.Collections.Generic;

namespace DOL.GS.Realm
{
	public class PlayerRace
	{
		public ERace ID { get; }
		public virtual eDAoCExpansion Expansion { get; }
		public ERealm Realm { get; }
		private ELivingModel FemaleModel { get; }
		private ELivingModel MaleModel { get; }

		private PlayerRace(ERace race, ERealm realm, eDAoCExpansion expansion, ELivingModel maleModel, ELivingModel femaleModel)
        {
			ID = race;
			Realm = realm;
			Expansion = expansion;
			MaleModel = maleModel;
			FemaleModel = femaleModel;
        }

		private static Dictionary<ERace, PlayerRace> races = new Dictionary<ERace, PlayerRace>()
		{
			{ ERace.Briton, new PlayerRace( ERace.Briton, ERealm.Albion, eDAoCExpansion.Classic, ELivingModel.BritonMale, ELivingModel.BritonFemale) } ,
			{ ERace.Highlander, new PlayerRace(ERace.Highlander, ERealm.Albion, eDAoCExpansion.Classic, ELivingModel.HighlanderMale, ELivingModel.HighlanderFemale) } ,
			{ ERace.Saracen, new PlayerRace(ERace.Saracen, ERealm.Albion, eDAoCExpansion.Classic, ELivingModel.SaracenMale, ELivingModel.SaracenFemale) } ,
			{ ERace.Avalonian, new PlayerRace(ERace.Avalonian, ERealm.Albion, eDAoCExpansion.Classic, ELivingModel.AvalonianMale, ELivingModel.AvalonianFemale) } ,
			{ ERace.Inconnu, new PlayerRace(ERace.Inconnu, ERealm.Albion, eDAoCExpansion.ShroudedIsles, ELivingModel.InconnuMale, ELivingModel.InconnuFemale) } ,
			{ ERace.HalfOgre, new PlayerRace(ERace.HalfOgre, ERealm.Albion, eDAoCExpansion.Catacombs, ELivingModel.HalfOgreMale, ELivingModel.HalfOgreFemale) } ,
			{ ERace.Korazh, new PlayerRace(ERace.Korazh, ERealm.Albion, eDAoCExpansion.LabyrinthOfTheMinotaur, ELivingModel.MinotaurMaleAlb, ELivingModel.None) },
			{ ERace.Troll, new PlayerRace(ERace.Troll, ERealm.Midgard, eDAoCExpansion.Classic, ELivingModel.TrollMale, ELivingModel.TrollFemale) },
			{ ERace.Norseman, new PlayerRace(ERace.Norseman, ERealm.Midgard, eDAoCExpansion.Classic, ELivingModel.NorseMale, ELivingModel.NorseFemale) } ,
			{ ERace.Kobold, new PlayerRace(ERace.Kobold, ERealm.Midgard, eDAoCExpansion.Classic, ELivingModel.KoboldMale, ELivingModel.KoboldFemale) } ,
			{ ERace.Dwarf, new PlayerRace(ERace.Dwarf, ERealm.Midgard, eDAoCExpansion.Classic, ELivingModel.DwarfMale, ELivingModel.DwarfFemale) } ,
			{ ERace.Valkyn, new PlayerRace(ERace.Valkyn, ERealm.Midgard, eDAoCExpansion.ShroudedIsles, ELivingModel.ValkynMale, ELivingModel.ValkynFemale) } ,
			{ ERace.Frostalf, new PlayerRace(ERace.Frostalf, ERealm.Midgard, eDAoCExpansion.Catacombs, ELivingModel.FrostalfMale, ELivingModel.FrostalfFemale) } ,
			{ ERace.Deifrang, new PlayerRace(ERace.Deifrang, ERealm.Midgard, eDAoCExpansion.LabyrinthOfTheMinotaur, ELivingModel.MinotaurMaleMid, ELivingModel.None) } ,
			{ ERace.Firbolg, new PlayerRace(ERace.Firbolg, ERealm.Hibernia, eDAoCExpansion.Classic, ELivingModel.FirbolgMale, ELivingModel.FirbolgFemale) } ,
			{ ERace.Celt, new PlayerRace(ERace.Celt, ERealm.Hibernia, eDAoCExpansion.Classic, ELivingModel.CeltMale, ELivingModel.CeltFemale) } ,
			{ ERace.Lurikeen, new PlayerRace(ERace.Lurikeen, ERealm.Hibernia, eDAoCExpansion.Classic, ELivingModel.LurikeenMale, ELivingModel.LurikeenFemale) } ,
			{ ERace.Elf, new PlayerRace(ERace.Elf, ERealm.Hibernia, eDAoCExpansion.Classic, ELivingModel.ElfMale, ELivingModel.ElfFemale) } ,
			{ ERace.Sylvan, new PlayerRace(ERace.Sylvan, ERealm.Hibernia, eDAoCExpansion.ShroudedIsles, ELivingModel.SylvanMale, ELivingModel.SylvanFemale) } ,
			{ ERace.Shar, new PlayerRace(ERace.Shar, ERealm.Hibernia, eDAoCExpansion.Catacombs, ELivingModel.SharMale, ELivingModel.SharFemale) } ,
			{ ERace.Graoch, new PlayerRace(ERace.Graoch, ERealm.Hibernia, eDAoCExpansion.LabyrinthOfTheMinotaur, ELivingModel.MinotaurMaleHib, ELivingModel.None) } ,
		};

		public ELivingModel GetModel(EGender gender)
        {
			if (gender == EGender.Male) return MaleModel;
			else if (gender == EGender.Female) return FemaleModel;
			else return ELivingModel.None;
		}

		public static List<PlayerRace> AllRaces
		{
			get
			{
				var allRaces = new List<PlayerRace>();
				foreach (var race in races)
				{
					allRaces.Add(race.Value);
				}
				return allRaces;
			}
		}

		public static PlayerRace Briton => races[ERace.Briton];
		public static PlayerRace Highlander => races[ERace.Highlander];
		public static PlayerRace Saracen => races[ERace.Saracen];
		public static PlayerRace Avalonian => races[ERace.Avalonian];
		public static PlayerRace Inconnu => races[ERace.Inconnu];
		public static PlayerRace HalfOgre => races[ERace.HalfOgre];
		public static PlayerRace Korazh => races[ERace.Korazh];
		public static PlayerRace Troll => races[ERace.Troll];
		public static PlayerRace Norseman => races[ERace.Norseman];
		public static PlayerRace Kobold => races[ERace.Kobold];
		public static PlayerRace Dwarf => races[ERace.Dwarf];
		public static PlayerRace Valkyn => races[ERace.Valkyn];
		public static PlayerRace Frostalf => races[ERace.Frostalf];
		public static PlayerRace Deifrang => races[ERace.Deifrang];
		public static PlayerRace Firbolg => races[ERace.Firbolg];
		public static PlayerRace Celt => races[ERace.Celt];
		public static PlayerRace Lurikeen => races[ERace.Lurikeen];
		public static PlayerRace Elf => races[ERace.Elf];
		public static PlayerRace Shar => races[ERace.Shar];
		public static PlayerRace Sylvan => races[ERace.Sylvan];
		public static PlayerRace Graoch => races[ERace.Graoch];

        public override bool Equals(object obj)
        {
            if(obj is PlayerRace compareRace)
            {
				return compareRace.ID == ID;
            }
			return false;
        }

        public override int GetHashCode()
        {
			return (int)ID;
        }
    }

	public enum eDAoCExpansion : byte
	{
		Classic = 1,
		ShroudedIsles = 2,
		TrialsOfAtlantis = 3,
		Catacombs = 4,
		DarknessRising = 5,
		LabyrinthOfTheMinotaur = 6
	}
}
