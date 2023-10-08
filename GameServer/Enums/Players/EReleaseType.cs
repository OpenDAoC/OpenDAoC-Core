namespace DOL.GS;

public enum EReleaseType
{
    /// <summary>
    /// Normal release to the bind point using /release command and 10sec delay after death
    /// </summary>
    Normal,
    /// <summary>
    /// Release to the players home city
    /// </summary>
    City,
    /// <summary>
    /// Release to the current location
    /// </summary>
    Duel,
    /// <summary>
    /// Release to your bind point
    /// </summary>
    Bind,
    /// <summary>
    /// Release in a battleground or the frontiers
    /// </summary>
    RvR,
    /// <summary>
    /// Release to players house
    /// </summary>
    House,
}