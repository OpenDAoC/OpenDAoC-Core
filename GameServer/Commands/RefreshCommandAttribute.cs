using System;

namespace Core.GS.Commands;

/// <summary>
/// Marks a class Method as Static Refresh Command.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RefreshCommandAttribute : Attribute
{
	public RefreshCommandAttribute()
	{
	}
}