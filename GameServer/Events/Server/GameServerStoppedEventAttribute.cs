using System;

namespace DOL.Events;

/// <summary>
/// This attribute can be applied to static methods to automatically
/// register them with the GameServer's global stop event
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
public class GameServerStoppedEventAttribute : Attribute
{
	/// <summary>
	/// Constructs a new GameServerStoppedEventAttribute
	/// </summary>
	public GameServerStoppedEventAttribute()
	{
	}
}