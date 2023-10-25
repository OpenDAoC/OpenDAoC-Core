namespace Core.GS.Enums;

/// <summary>
/// Holds the possible activeWeaponSlot values
/// </summary>
public enum EActiveWeaponSlot : byte
{
    /// <summary>
    /// Weapon slot righthand
    /// </summary>
    Standard = 0x00,
    /// <summary>
    /// Weaponslot twohanded
    /// </summary>
    TwoHanded = 0x01,
    /// <summary>
    /// Weaponslot distance
    /// </summary>
    Distance = 0x02
}