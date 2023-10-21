namespace Core.Database.Enums;

/// <summary>
/// Connection isolation levels
/// </summary>
public enum EIsolationLevel
{
	DEFAULT,
	SERIALIZABLE,
    REPEATABLE_READ,
	READ_COMMITTED,
    READ_UNCOMMITTED,
	SNAPSHOT
}