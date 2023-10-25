namespace Core.GS.Enums;

/// <summary>
/// Current state of the client
/// </summary>
public enum EClientState
{
    NotConnected = 0x00,
    Connecting = 0x01,
    CharScreen = 0x02,
    WorldEnter = 0x03,
    Playing = 0x04,
    Linkdead = 0x05,
    Disconnected = 0x06,
}