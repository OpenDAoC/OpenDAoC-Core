namespace Core.GS;

/// <summary>
/// The possible results for prechecks for range attacks
/// </summary>
public enum ECheckRangeAttackStateResult
{
    /// <summary>
    /// Hold the shot/throw
    /// </summary>
    Hold,
    /// <summary>
    /// Fire the shot/throw
    /// </summary>
    Fire,
    /// <summary>
    /// Stop the attack
    /// </summary>
    Stop
}