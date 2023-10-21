namespace Core.GS.Database;

/// <summary>
/// Interface for all database format converters
/// </summary>
public interface IDbConverter
{
	/// <summary>
	/// Converts the database to new version specified in attribute
	/// </summary>
	void ConvertDatabase();
}