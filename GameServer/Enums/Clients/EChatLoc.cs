namespace Core.GS.PacketHandler;

/// <summary>
/// Chat locations on the client window
/// </summary>
public enum EChatLoc : byte
{
    CL_ChatWindow = 0x0,
    CL_PopupWindow = 0x1,
    CL_SystemWindow = 0x2
}