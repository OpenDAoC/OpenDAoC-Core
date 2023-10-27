﻿using System.Text;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Spells;

namespace Core.GS.Skills;

public abstract class Skill
{
	protected int m_id;
	protected string m_name;
	protected int m_level;
	protected ushort m_icon;
	protected int m_internalID;

	/// <summary>
	/// Construct a Skill from the name, an id, and a level
	/// </summary>
	/// <param name="name"></param>
	/// <param name="id"></param>
	/// <param name="level"></param>
	public Skill(string name, int id, ushort icon, int level, int internalID)
	{
		m_id = id;
		m_name = name;
		m_level = level;
		m_icon = icon;
		m_internalID = internalID;
	}

	/// <summary>
	/// in most cases it is icon id or other specifiing id for client
	/// like spell id or style id in spells
	/// </summary>
	public virtual int ID
	{
		get { return m_id; }
	}

	/// <summary>
	/// The Skill Name
	/// </summary>
	public virtual string Name
	{
		get { return m_name; }
		set { m_name = value; }
	}

	/// <summary>
	/// The Skill Level
	/// </summary>
	public virtual int Level
	{
		get { return m_level; }
		set { m_level = value; }
	}

	/// <summary>
	/// The Skill Icon
	/// </summary>
	public virtual ushort Icon 
	{
		get { return m_icon; }
	}
	
	/// <summary>
	/// Internal ID is used for Hardcoded Tooltip.
	/// </summary>
	public virtual int InternalID {
		get { return m_internalID; }
		set { m_internalID = value; }
	}

	/// <summary>
	/// the type of the skill
	/// </summary>
	public virtual ESkillPage SkillType
	{
		get { return ESkillPage.Abilities; }
	}

	/// <summary>
	/// Clone a skill
	/// </summary>
	/// <returns></returns>
	public virtual Skill Clone()
	{
		return (Skill)MemberwiseClone();
	}

	public override int GetHashCode()
	{
		return m_id;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
			return false;

		if (ReferenceEquals(obj, this))
			return true;

		if (GetType() != obj.GetType())
			return false;

		return m_id == (obj as Skill).m_id;
	}

	public static bool operator == (Skill left, Skill right)
	{
		return left is null ? right is null : left.Equals(right);
	}

	public static bool operator != (Skill left, Skill right)
	{
		return !(left == right);
	}
}

/// <summary>
/// the named skill is used for identification purposes
/// the name is strong and must be unique for one type of skill page
/// so better make the name real unique
/// </summary>
public class NamedSkill : Skill
{
	private string m_keyName;

	/// <summary>
	/// Construct a named skill from the keyname, name, id and level
	/// </summary>
	/// <param name="keyName">The keyname</param>
	/// <param name="name">The name</param>
	/// <param name="id">The ID</param>
	/// <param name="level">The level</param>
	public NamedSkill(string keyName, string name, int id, ushort icon, int level, int internalID)
		: base(name, id, icon, level, internalID)
	{
		m_keyName = keyName;
	}

	/// <summary>
	/// Returns the string representation of the Skill
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return new StringBuilder(32)
			.Append("KeyName=").Append(KeyName)
			.Append(", ID=").Append(ID)
			.Append(", Icon=").Append(Icon)
			.Append(", Level=").Append(Level)
			.ToString();
	}

	/// <summary>
	/// strong identification name
	/// </summary>
	public virtual string KeyName
	{
		get { return m_keyName; }
	}
}

public class Song : Spell
{
	public Song(DbSpell spell, int requiredLevel)
		: base(spell, requiredLevel)
	{
	}

	public override ESkillPage SkillType
	{
		get { return ESkillPage.Songs; }
	}
	
	public override Skill Clone()
	{
		return (Song)MemberwiseClone();
	}

}

public class SpellLine : NamedSkill
{
	protected bool m_isBaseLine;
	protected string m_spec;

	public SpellLine(string keyname, string name, string spec, bool baseline) : this(keyname, name, 0, spec, baseline)
	{
		m_isBaseLine = baseline;
		m_spec = spec;
	}

	public SpellLine(string keyname, string name, int id, string spec, bool baseline) : base(keyname, name, id, 0, 1, 0)
	{
		m_isBaseLine = baseline;
		m_spec = spec;
	}

	public string Spec
	{
		get { return m_spec; }
	}

	public bool IsBaseLine
	{
		get { return m_isBaseLine; }
	}

	public override ESkillPage SkillType
	{
		get { return ESkillPage.Spells; }
	}

	public override Skill Clone()
	{
		return (SpellLine)MemberwiseClone();
	}
}