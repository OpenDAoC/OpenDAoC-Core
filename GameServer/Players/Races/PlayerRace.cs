using System.Collections.Generic;
using Core.GS.Enums;

namespace Core.GS.Players;

public class PlayerRace
{
	public ERace ID { get; }
	public virtual EDAoCExpansion Expansion { get; }
	public ERealm Realm { get; }
	private ELivingModel FemaleModel { get; }
	private ELivingModel MaleModel { get; }

	private PlayerRace(ERace race, ERealm realm, EDAoCExpansion expansion, ELivingModel maleModel, ELivingModel femaleModel)
    {
		ID = race;
		Realm = realm;
		Expansion = expansion;
		MaleModel = maleModel;
		FemaleModel = femaleModel;
    }

	private static Dictionary<ERace, PlayerRace> races = new Dictionary<ERace, PlayerRace>()
	{
		{ ERace.Briton, new PlayerRace( ERace.Briton, ERealm.Albion, EDAoCExpansion.Classic, ELivingModel.BritonMale, ELivingModel.BritonFemale) } ,
		{ ERace.Highlander, new PlayerRace(ERace.Highlander, ERealm.Albion, EDAoCExpansion.Classic, ELivingModel.HighlanderMale, ELivingModel.HighlanderFemale) } ,
		{ ERace.Saracen, new PlayerRace(ERace.Saracen, ERealm.Albion, EDAoCExpansion.Classic, ELivingModel.SaracenMale, ELivingModel.SaracenFemale) } ,
		{ ERace.Avalonian, new PlayerRace(ERace.Avalonian, ERealm.Albion, EDAoCExpansion.Classic, ELivingModel.AvalonianMale, ELivingModel.AvalonianFemale) } ,
		{ ERace.Inconnu, new PlayerRace(ERace.Inconnu, ERealm.Albion, EDAoCExpansion.ShroudedIsles, ELivingModel.InconnuMale, ELivingModel.InconnuFemale) } ,
		{ ERace.HalfOgre, new PlayerRace(ERace.HalfOgre, ERealm.Albion, EDAoCExpansion.Catacombs, ELivingModel.HalfOgreMale, ELivingModel.HalfOgreFemale) } ,
		{ ERace.Korazh, new PlayerRace(ERace.Korazh, ERealm.Albion, EDAoCExpansion.LabyrinthOfTheMinotaur, ELivingModel.MinotaurMaleAlb, ELivingModel.None) },
		{ ERace.Troll, new PlayerRace(ERace.Troll, ERealm.Midgard, EDAoCExpansion.Classic, ELivingModel.TrollMale, ELivingModel.TrollFemale) },
		{ ERace.Norseman, new PlayerRace(ERace.Norseman, ERealm.Midgard, EDAoCExpansion.Classic, ELivingModel.NorseMale, ELivingModel.NorseFemale) } ,
		{ ERace.Kobold, new PlayerRace(ERace.Kobold, ERealm.Midgard, EDAoCExpansion.Classic, ELivingModel.KoboldMale, ELivingModel.KoboldFemale) } ,
		{ ERace.Dwarf, new PlayerRace(ERace.Dwarf, ERealm.Midgard, EDAoCExpansion.Classic, ELivingModel.DwarfMale, ELivingModel.DwarfFemale) } ,
		{ ERace.Valkyn, new PlayerRace(ERace.Valkyn, ERealm.Midgard, EDAoCExpansion.ShroudedIsles, ELivingModel.ValkynMale, ELivingModel.ValkynFemale) } ,
		{ ERace.Frostalf, new PlayerRace(ERace.Frostalf, ERealm.Midgard, EDAoCExpansion.Catacombs, ELivingModel.FrostalfMale, ELivingModel.FrostalfFemale) } ,
		{ ERace.Deifrang, new PlayerRace(ERace.Deifrang, ERealm.Midgard, EDAoCExpansion.LabyrinthOfTheMinotaur, ELivingModel.MinotaurMaleMid, ELivingModel.None) } ,
		{ ERace.Firbolg, new PlayerRace(ERace.Firbolg, ERealm.Hibernia, EDAoCExpansion.Classic, ELivingModel.FirbolgMale, ELivingModel.FirbolgFemale) } ,
		{ ERace.Celt, new PlayerRace(ERace.Celt, ERealm.Hibernia, EDAoCExpansion.Classic, ELivingModel.CeltMale, ELivingModel.CeltFemale) } ,
		{ ERace.Lurikeen, new PlayerRace(ERace.Lurikeen, ERealm.Hibernia, EDAoCExpansion.Classic, ELivingModel.LurikeenMale, ELivingModel.LurikeenFemale) } ,
		{ ERace.Elf, new PlayerRace(ERace.Elf, ERealm.Hibernia, EDAoCExpansion.Classic, ELivingModel.ElfMale, ELivingModel.ElfFemale) } ,
		{ ERace.Sylvan, new PlayerRace(ERace.Sylvan, ERealm.Hibernia, EDAoCExpansion.ShroudedIsles, ELivingModel.SylvanMale, ELivingModel.SylvanFemale) } ,
		{ ERace.Shar, new PlayerRace(ERace.Shar, ERealm.Hibernia, EDAoCExpansion.Catacombs, ELivingModel.SharMale, ELivingModel.SharFemale) } ,
		{ ERace.Graoch, new PlayerRace(ERace.Graoch, ERealm.Hibernia, EDAoCExpansion.LabyrinthOfTheMinotaur, ELivingModel.MinotaurMaleHib, ELivingModel.None) } ,
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