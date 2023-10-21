using System;

namespace DOL.Events;

/// <summary>
/// This attribute can be applied to static methods to automatically
/// register them with the GameServer's global script compiled event
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
public class ScriptLoadedEventAttribute : Attribute
{
	/// <summary>
	/// Constructs a new ScriptLoadedEventAttribute
	/// </summary>
	public ScriptLoadedEventAttribute()
	{
	}
}