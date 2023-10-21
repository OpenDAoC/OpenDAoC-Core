namespace Core.GS;

/// <summary>
/// Holds the weapon damage type
/// </summary>
public enum EWeaponDamageType : byte
{
	Elemental = 0,
	Crush = 1,
	Slash = 2,
	Thrust = 3,

	Body = 10,
	Cold = 11,
	Energy = 12,
	Heat = 13,
	Matter = 14,
	Spirit = 15,
}