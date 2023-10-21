using System;
using Core.GS;
using Core.GS.Enums;
using Core.GS.Keeps;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the Keep event
/// </summary>
public class KeepEventArgs : EventArgs
{

	/// <summary>
	/// The keep
	/// </summary>
	private AGameKeep m_keep;

	/// <summary>
	/// The realm
	/// </summary>
	private ERealm m_realm;

	/// <summary>
	/// Constructs a new KeepEventArgs
	/// </summary>
	public KeepEventArgs(AGameKeep keep)
	{
		this.m_keep = keep;
	}

	public KeepEventArgs(AGameKeep keep, ERealm realm)
	{
		this.m_keep = keep;
		this.m_realm = realm;
	}

	/// <summary>
	/// Gets the Keep
	/// </summary>
	public AGameKeep Keep
	{
		get { return m_keep; }
	}

	/// <summary>
	/// Gets the Realm
	/// </summary>
	public ERealm Realm
	{
		get { return m_realm; }
	}
}