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

		private static Dictionary<short, RaceStats> Stats = new Dictionary<short, RaceStats>()
		{
			{0, new RaceStats(30, 30, 30, 30, 30, 30, 30, 30)}, //Unknown
			{1, new RaceStats(60, 60, 60, 60, 60, 60, 60, 60)}, //Briton
			{2, new RaceStats(45, 45, 60, 70, 80, 60, 60, 60)}, //Avalonian
			{3, new RaceStats(70, 70, 50, 50, 60, 60, 60, 60)}, //Highlander
			{4, new RaceStats(50, 50, 80, 60, 60, 60, 60, 60)}, //Saracen
			{5, new RaceStats(70, 70, 50, 50, 60, 60, 60, 60)}, //Norseman
			{6, new RaceStats(100, 70, 35, 35, 60, 60, 60, 60)}, //Troll
			{7, new RaceStats(60, 80, 50, 50, 60, 60, 60, 60)}, //Dwarf
			{8, new RaceStats(50, 50, 70, 70, 60, 60, 60, 60)}, //briton
			{9, new RaceStats(60, 60, 60, 60, 60, 60, 60, 60)}, //Celt
			{10, new RaceStats(90, 60, 40, 40, 60, 60, 70, 60)}, //Firbolg
			{11, new RaceStats(40, 40, 75, 75, 70, 60, 60, 60)}, //Elf
			{12, new RaceStats(40, 40, 80, 80, 60, 60, 60, 60)}, //Lurikeen
			{13, new RaceStats(50, 60, 70, 50, 70, 60, 60, 60)}, //Inconnu
			{14, new RaceStats(55, 45, 65, 75, 60, 60, 60, 60)}, //Valkyn
			{15, new RaceStats(70, 60, 55, 45, 70, 60, 60, 60)}, //Sylvan
			{16, new RaceStats(90, 70, 40, 40, 60, 60, 60, 60)}, //HalfOgre
			{17, new RaceStats(55, 55, 55, 60, 60, 75, 60, 60)}, //Frostalf
			{18, new RaceStats(60, 80, 50, 50, 60, 60, 60, 60)}, //Shar
			{19, new RaceStats(80, 70, 50, 40, 60, 60, 60, 60)}, // alb minotaur
			{20, new RaceStats(80, 70, 50, 40, 60, 60, 60, 60)}, // mid minotaur
			{21, new RaceStats(80, 70, 50, 40, 60, 60, 60, 60)} // hib minotaur
		};
		
		public static RaceStats GetRaceStats(short race)
		{
			return !Stats.ContainsKey(race) ? null : Stats[race];
		}
	}
	
}
