namespace DOL.GS;

/// <summary>
/// The type of player class
/// </summary>
public enum EPlayerClassType : int
{
    /// <summary>
    /// The class has access to all spells
    /// </summary>
    ListCaster,
    /// <summary>
    /// The class has access to best one or two spells
    /// </summary>
    Hybrid,
    /// <summary>
    /// The class has no spells
    /// </summary>
    PureTank,
}