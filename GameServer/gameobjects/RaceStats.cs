using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Realm
{
	public class RaceStats
	{
		public int Strength;
		public int Constitution;
		public int Dexterity;
		public int Quickness;
		public int Intelligence;
		public int Empathy;
		public int Piety;
		public int Charisma;

		public RaceStats(int str, int con, int dex, int qui, int intel, int pie, int emp, int charisma)
		{
			Strength = str;
			Constitution = con;
			Dexterity = dex;
			Quickness = qui;
			Intelligence = intel;
			Piety = pie;
			Empathy = emp;
			Charisma = charisma;
		}

		private static Dictionary<PlayerRace, RaceStats> Stats = new Dictionary<PlayerRace, RaceStats>()
		{
			{PlayerRace.Avalonian, new RaceStats(45, 45, 60, 70, 80, 60, 60, 60)},
			{PlayerRace.Highlander, new RaceStats(70, 70, 50, 50, 60, 60, 60, 60)},
			{PlayerRace.Saracen, new RaceStats(50, 50, 80, 60, 60, 60, 60, 60)},
			{PlayerRace.Norseman, new RaceStats(70, 70, 50, 50, 60, 60, 60, 60)},
			{PlayerRace.Troll, new RaceStats(100, 70, 35, 35, 60, 60, 60, 60)},
			{PlayerRace.Dwarf, new RaceStats(60, 80, 50, 50, 60, 60, 60, 60)},
			{PlayerRace.Kobold, new RaceStats(50, 50, 70, 70, 60, 60, 60, 60)},
			{PlayerRace.Celt, new RaceStats(60, 60, 60, 60, 60, 60, 60, 60)},
			{PlayerRace.Firbolg, new RaceStats(90, 60, 40, 40, 60, 60, 70, 60)},
			{PlayerRace.Elf, new RaceStats(40, 40, 75, 75, 70, 60, 60, 60)},
			{PlayerRace.Lurikeen, new RaceStats(40, 40, 80, 80, 60, 60, 60, 60)},
			{PlayerRace.Inconnu, new RaceStats(50, 60, 70, 50, 70, 60, 60, 60)},
			{PlayerRace.Valkyn, new RaceStats(55, 45, 65, 75, 60, 60, 60, 60)},
			{PlayerRace.Sylvan, new RaceStats(70, 60, 55, 45, 70, 60, 60, 60)},
			{PlayerRace.HalfOgre, new RaceStats(90, 70, 40, 40, 60, 60, 60, 60)},
			{PlayerRace.Frostalf, new RaceStats(55, 55, 55, 60, 60, 75, 60, 60)},
			{PlayerRace.Shar, new RaceStats(60, 80, 50, 50, 60, 60, 60, 60)},
			{PlayerRace.Korazh, new RaceStats(80, 70, 50, 40, 60, 60, 60, 60)}, // alb minotaur
			{PlayerRace.Deifrang, new RaceStats(80, 70, 50, 40, 60, 60, 60, 60)}, // mid minotaur
			{PlayerRace.Graoch, new RaceStats(80, 70, 50, 40, 60, 60, 60, 60)} // hib minotaur
		};
		
		public static RaceStats GetRaceStats(PlayerRace race)
		{
			return !Stats.ContainsKey(race) ? null : Stats[race];
		}
	}
	
}
