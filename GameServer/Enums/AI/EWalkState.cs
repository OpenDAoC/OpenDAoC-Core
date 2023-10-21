namespace Core.GS.Enums;

/// <summary>
/// Defines walk state when brain is not in combat
/// </summary>
public enum EWalkState
{
    /// <summary>
    /// Follow the owner
    /// </summary>
    Follow,
    /// <summary>
    /// Don't move if not in combat
    /// </summary>
    Stay,
    ComeHere,
    GoTarget,
}