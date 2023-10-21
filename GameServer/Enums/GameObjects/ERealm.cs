namespace Core.GS.Enums;

/// <summary>
/// Defines the realms for various packets and search functions etc.
/// </summary>
public enum ERealm : byte
{
	/// <summary>
	/// First realm number, for use in all arrays
	/// </summary>
	_First = 0,
	/// <summary>
	/// No specific realm
	/// </summary>
	None = 0,
	/// <summary>
	/// First player realm number, for use in all arrays
	/// </summary>
	_FirstPlayerRealm = 1,
	/// <summary>
	/// Albion Realm
	/// </summary>
	Albion = 1,
	/// <summary>
	/// Midgard Realm
	/// </summary>
	Midgard = 2,
	/// <summary>
	/// Hibernia Realm
	/// </summary>
	Hibernia = 3,
	/// <summary>
	/// Last player realm number, for use in all arrays
	/// </summary>
	_LastPlayerRealm = 3,

	/// <summary>
	/// LastRealmNumber to allow dynamic allocation of realm specific arrays.
	/// </summary>
	_Last = 3,
	
	/// <summary>
	/// DoorRealmNumber to allow dynamic allocation of realm specific arrays.
	/// </summary>
	Door = 6,
}