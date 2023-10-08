namespace DOL.GS;

/// <summary>
/// Holds all the damage types that
/// some attack may cause on the target
/// </summary>
public enum EDamageType : byte
{
	_FirstResist = 0,
	Natural = 0,
	Crush = 1,
	Slash = 2,
	Thrust = 3,

	Body = 10,
	Cold = 11,
	Energy = 12,
	Heat = 13,
	Matter = 14,
	Spirit = 15,
	_LastResist = 15,
	/// <summary>
	/// Damage is from a GM via a command
	/// </summary>
	GM = 254,
	/// <summary>
	/// Player is taking falling damage
	/// </summary>
	Falling = 255,
}