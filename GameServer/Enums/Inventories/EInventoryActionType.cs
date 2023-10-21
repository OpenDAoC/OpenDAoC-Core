namespace Core.GS;

/// <summary>
/// Type of trade
/// </summary>
public enum EInventoryActionType
{
    /// <summary>
    /// Trade between 2 players
    /// </summary>
    Trade,
    /// <summary>
    /// A player pick up a loot
    /// </summary>
    Loot,
    /// <summary>
    /// Gain of a quest or quest's items
    /// </summary>
    Quest,
    /// <summary>
    /// Buy/sell an item
    /// </summary>
    Merchant,
    /// <summary>
    /// Crafting an item
    /// </summary>
    Craft,
    /// <summary>
    /// Any other action
    /// </summary>
    Other,
}