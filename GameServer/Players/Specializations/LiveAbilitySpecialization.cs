using System.Collections.Generic;
using Core.GS.Skills;
using Core.GS.Styles;

namespace Core.GS.Players;

public class LiveAbilitySpecialization : Specialization
{
	public LiveAbilitySpecialization(string keyname, string displayname, ushort icon, int ID)
		: base(keyname, displayname, icon, ID)
	{
	}
	
	/// <summary>
	/// No Spells for Ability Specs.
	/// </summary>
	/// <param name="living"></param>
	/// <returns></returns>
	protected override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living, int level)
	{
		return new Dictionary<SpellLine, List<Skill>>();
	}
	
	/// <summary>
	/// No Spells for Ability Specs.
	/// </summary>
	/// <param name="living"></param>
	/// <returns></returns>
	protected override List<SpellLine> GetSpellLinesForLiving(GameLiving living, int level)
	{
		return new List<SpellLine>();
	}
	
	/// <summary>
	/// No Styles for Ability Specs.
	/// </summary>
	/// <param name="living"></param>
	/// <returns></returns>
	protected override List<Style> GetStylesForLiving(GameLiving living, int level)
	{
		return new List<Style>();
	}
}