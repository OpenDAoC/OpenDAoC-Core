namespace Core.GS.PacketHandler;

/// <summary>
/// Enum for LoginDeny reasons
/// </summary>
public enum ELoginError : byte
{
    // From testing with client version 1.98 US:
    // All of these values send no message (client just displays "Service not available"):
    // 0x04, 0x0e, 0x0f, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
    WrongPassword = 0x01,
    AccountInvalid = 0x02,
    AuthorizationServerUnavailable = 0x03,
    ClientVersionTooLow = 0x05,
    CannotAccessUserAccount = 0x06,
    AccountNotFound = 0x07,
    AccountNoAccessAnyGame = 0x08,
    AccountNoAccessThisGame = 0x09,
    AccountClosed = 0x0a,
    AccountAlreadyLoggedIn = 0x0b,
    TooManyPlayersLoggedIn = 0x0c,
    GameCurrentlyClosed = 0x0d,
    AccountAlreadyLoggedIntoOtherServer = 0x10,
    AccountIsInLogoutProcedure = 0x11,
    ExpansionPacketNotAllowed = 0x12, // "You have not been invited to join this server type." (1.98 US)
    AccountIsBannedFromThisServerType = 0x13,
    CafeIsOutOfPlayingTime = 0x14,
    PersonalAccountIsOutOfTime = 0x15,
    CafesAccountIsSuspended = 0x16,
    NotAuthorizedToUseExpansionVersion = 0x17, // "You are not authorized to use the expansion version!" (1.98 US)
    ServiceNotAvailable = 0xaa
}