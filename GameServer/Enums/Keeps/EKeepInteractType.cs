namespace DOL.GS.Keeps;

/// <summary>
/// The type of interaction we check for to handle lord permission checks
/// </summary>
public enum EKeepInteractType
{ 
    /// <summary>
    /// Claim the Area
    /// </summary>
    Claim,
    /// <summary>
    /// Release the Area
    /// </summary>
    Release,
    /// <summary>
    /// Change the level of the Area
    /// </summary>
    ChangeLevel,
}