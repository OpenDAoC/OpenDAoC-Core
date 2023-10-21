using System;

namespace DOL.Events;

/// <summary>
/// This attribute can be applied to static methods to automatically
/// register them with the GameServer's global start event
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
public class GameServerStartedEventAttribute : Attribute
{
	/// <summary>
	/// Constructs a new GameServerStartedEventAttribute
	/// </summary>
	public GameServerStartedEventAttribute()
	{
	}
}