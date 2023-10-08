using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
	/// <summary>
	/// MiniLineSpecialization are "Mini-Spec" Used to match Sub-Spec (CL ~ Subclass) Skills
	/// They shouldn't be attached to a career, Global Champion or other Custom Career will handle them and display skills.
	/// </summary>
	public class MiniLineSpecialization : UntrainableSpecialization
	{		
		public MiniLineSpecialization(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
		
		/// <summary>
		/// Always Empty Collection
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public override List<Ability> GetAbilitiesForLiving(GameLiving living)
		{
			return new List<Ability>();
		}
		
		/// <summary>
		/// Always Empty Collection
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public override List<SpellLine> GetSpellLinesForLiving(GameLiving living)
		{
			return new List<SpellLine>();
		}
		
		/// <summary>
		/// Always Empty Collection
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living)
		{
			return new Dictionary<SpellLine, List<Skill>>();
		}
		
		/// <summary>
		/// Always Empty Collection
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public override List<DOL.GS.Styles.Style> GetStylesForLiving(GameLiving living)
		{
			return new List<DOL.GS.Styles.Style>();
		}
		
		/// <summary>
		/// Retrieve the Mini Spec Skill List, it will be used as a 0-index skill line
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public virtual List<Tuple<Skill, Skill>> GetMiniLineSkillsForLiving(GameLiving living, int level)
		{
			var abilities = base.PretendAbilitiesForLiving(living, level);
			var styles = base.PretendStylesForLiving(living, level);
			var spells = base.PretendLinesSpellsForLiving(living, level);

			return abilities.Select(a => new Tuple<Skill, Skill, int>(a, null, a.SpecLevelRequirement))
				.Union(styles.Select(s => new Tuple<Skill, Skill, int>(s, null, s.SpecLevelRequirement)))
				.Union(spells.Select(kv => kv.Value.Select(e => new Tuple<Skill, Skill, int>(e, kv.Key, e.Level))).SelectMany(e => e))
				.OrderBy(t => t.Item3).Select(sk => new Tuple<Skill, Skill>(sk.Item1, sk.Item2)).Take(level).ToList();
		}
	}
	
	/// <summary>
	/// LiveChampionLineSpec are Mini-Lines that match a base class type
	/// Each "Grouped" Spec that are all displayed by the same trainer should use the same class
	/// Subclass is only used for grouping in Database, removing the need for a dedicated table
	/// Each Spec will be save to the according level to keep track of choosen paths.
	/// </summary>
	public class LiveChampionLevelsLineSpec : MiniLineSpecialization
	{
		public LiveChampionLevelsLineSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLAcolyteSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLAcolyteSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLAlbionRogueSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLAlbionRogueSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLDiscipleSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLDiscipleSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLElementalistSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLElementalistSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLFighterSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLFighterSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLForesterSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLForesterSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLGuardianSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLGuardianSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLMageSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLMageSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLMagicianSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLMagicianSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLMidgardRogueSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLMidgardRogueSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLMysticSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLMysticSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLNaturalistSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLNaturalistSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLSeerSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLSeerSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLStalkerSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLStalkerSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
	public class LiveCLVikingSpec : LiveChampionLevelsLineSpec
	{
		public LiveCLVikingSpec(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
	}
}