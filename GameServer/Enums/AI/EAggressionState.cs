namespace DOL.AI.Brain;

/// <summary>
/// Defines aggression level of the brain
/// </summary>
public enum EAggressionState
{
    /// <summary>
    /// Attack any enemy in range
    /// </summary>
    Aggressive,
    /// <summary>
    /// Attack anything that attacks brain owner or owner of brain owner
    /// </summary>
    Defensive,
    /// <summary>
    /// Attack only on order
    /// </summary>
    Passive,
}