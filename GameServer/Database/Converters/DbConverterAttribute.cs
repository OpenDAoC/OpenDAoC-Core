using System;

namespace DOL.GS.DatabaseConverters;

/// <summary>
/// Attribute that denotes a class as a database converter
/// from previous version to the specified in attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
public class DbConverterAttribute : Attribute
{
	private int m_targetVersion;

	/// <summary>
	/// Constructs new attribute for database converter classes
	/// </summary>
	/// <param name="targetVersion">Target database version after convertion</param>
	public DbConverterAttribute(int targetVersion)
	{
		m_targetVersion = targetVersion;
	}

	public int TargetVersion
	{
		get { return m_targetVersion; }
	}
}