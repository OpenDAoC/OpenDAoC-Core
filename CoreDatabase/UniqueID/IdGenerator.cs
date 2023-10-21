using System;

namespace Core.Database;

/// <summary>
/// Generates a unique ID for every object.
/// </summary>
public static class IdGenerator
{
	/// <summary>
	/// Generate a new GUID String
	/// </summary>
	/// <returns>a new unique Key</returns>
	public static string GenerateID()
	{
		return Guid.NewGuid().ToString();
	}
}