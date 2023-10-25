using System.Collections.Generic;
using Core.GS.Skills;

namespace Core.GS.Players;

public class LiveWeaponSpecialization : Specialization
{
	public LiveWeaponSpecialization(string keyname, string displayname, ushort icon, int ID)
		: base(keyname, displayname, icon, ID)
	{
	}
	
	/// <summary>
	/// No Spells for Weapon Specs.
	/// </summary>
	/// <param name="living"></param>
	/// <returns></returns>
	protected override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living, int level)
	{
		return new Dictionary<SpellLine, List<Skill>>();
	}
	
	/// <summary>
	/// No Spells for Weapon Specs.
	/// </summary>
	/// <param name="living"></param>
	/// <returns></returns>
	protected override List<SpellLine> GetSpellLinesForLiving(GameLiving living, int level)
	{
		return new List<SpellLine>();
	}
}