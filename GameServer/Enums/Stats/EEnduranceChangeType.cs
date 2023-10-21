namespace Core.GS;

/// <summary>
/// Holds all the ways this living can
/// be healed
/// </summary>
public enum EEnduranceChangeType : byte
{
    /// <summary>
    /// Enduracen was changed by unknown
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Endurance was changed by Regenerate
    /// </summary>
    Regenerate = 1,
    /// <summary>
    /// Enduracen was changed by spell
    /// </summary>
    Spell = 2,
    /// <summary>
    /// Enduracen was changed by potion
    /// </summary>
    Potion = 3
}