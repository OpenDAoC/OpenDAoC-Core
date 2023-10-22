using System;
using Core.GS.Enums;

namespace Core.GS;

/// <summary>
/// Marks a class as a guild-wide NPC script
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NpcGuildScriptAttribute : Attribute
{
	string m_guild;
	ERealm m_realm;

	/// <summary>
	/// constructs new attribute
	/// </summary>
	/// <param name="guildname">name of the npc guild to that the script has to be applied</param>
	/// <param name="realm">valid realm for the script</param>
	public NpcGuildScriptAttribute(string guildname, ERealm realm)
	{
		m_guild = guildname;
		m_realm = realm;
	}

	/// <summary>
	/// constructs new attribute
	/// </summary>
	/// <param name="guildname">name of the npc guild to that the script has to be applied</param>
	public NpcGuildScriptAttribute(string guildname)
	{
		m_guild = guildname;
		m_realm = ERealm.None;
	}

	/// <summary>
	/// npc guild
	/// </summary>
	public string GuildName {
		get { return m_guild; }
	}

	/// <summary>
	/// valid realm for this script
	/// </summary>
	public ERealm Realm {
		get { return m_realm; }
	}
}