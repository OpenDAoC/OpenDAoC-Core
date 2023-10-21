namespace Core.GS.DatabaseUpdate;

/// <summary>
/// Interface for all database updaters
/// </summary>
public interface IDbUpdater
{
	/// <summary>
	/// Converts the database to new version specified in attribute
	/// </summary>
	void Update();
}