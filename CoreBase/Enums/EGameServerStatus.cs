namespace DOL;

/// <summary>
/// The status of the gameserver
/// </summary>
public enum EGameServerStatus
{
    /// <summary>
    /// Server is open for connections
    /// </summary>
    GSS_Open = 0,
    /// <summary>
    /// Server is closed and won't accept connections
    /// </summary>
    GSS_Closed,
    /// <summary>
    /// Server is down
    /// </summary>
    GSS_Down,
    /// <summary>
    /// Server is full, no more connections accepted
    /// </summary>
    GSS_Full,
    /// <summary>
    /// Unknown server status
    /// </summary>
    GSS_Unknown,
    /// <summary>
    /// Server is banned for the user
    /// </summary>
    GSS_Banned,
    /// <summary>
    /// User is not invited
    /// </summary>
    GSS_NotInvited,
    /// <summary>
    /// The count of server stati
    /// </summary>
    _GSS_Count,
}