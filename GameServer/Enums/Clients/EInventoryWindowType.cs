namespace DOL.GS.PacketHandler;

/// <summary>
/// The type of inventory window to show the client.  
/// Update (0) for updating the inventory on the client without displaying a window
/// </summary>
public enum EInventoryWindowType : byte
{
    /// <summary>
    /// 0x00: Send an inventory update to the client, no window
    /// </summary>
    Update = 0x00,
    /// <summary>
    /// 0x01: Send all equippable slots
    /// </summary>
    Equipment = 0x01,
    /// <summary>
    /// 0x02: Send all non equippable slots
    /// </summary>
    Inventory = 0x02,
    /// <summary>
    /// 0x03: Send player vault window
    /// </summary>
    PlayerVault = 0x03,
    /// <summary>
    /// 0x04: Send housing vault window
    /// </summary>
    HouseVault = 0x04,
    /// <summary>
    /// 0x05: Send Consignment window in owner mode showing consignment money
    /// </summary>
    ConsignmentOwner = 0x05,
    /// <summary>
    /// 0x06: Send Consignment window in viewer / buy mode
    /// </summary>
    ConsignmentViewer = 0x06,
    /// <summary>
    /// 0x07: HorseBags
    /// </summary>
    HorseBags = 0x07,
}