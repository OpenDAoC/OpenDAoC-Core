namespace Core.GS.Enums;

/// <summary>
/// The type of attack
/// </summary>
public enum EAttackType : int
{
    /// <summary>
    /// Attack type has not been set yet
    /// </summary>
    Unknown = -1,
    /// <summary>
    /// Attack is done using a weapon in one hand
    /// </summary>
    MeleeOneHand = 1,
    /// <summary>
    /// Attack is done using one weapon in each hand
    /// </summary>
    MeleeDualWield = 2,
    /// <summary>
    /// Attack is done using one same weapon in both hands
    /// </summary>
    MeleeTwoHand = 3,
    /// <summary>
    /// Attack is done using a weapon in ranged slot
    /// </summary>
    Ranged = 4,
    /// <summary>
    /// Attack is done with a spell
    /// </summary>
    Spell = 5,
}