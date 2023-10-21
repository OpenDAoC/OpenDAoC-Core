namespace Core.GS.PacketHandler;

public enum EMerchantWindowType : byte
{
    Normal = 0x00,
    Bp = 0x01,
    Count = 0x02,
    HousingDeedMenu = 0x03,
    HousingOutsideMenu = 0x04,
    HousingNPCHookpoint = 0x05,
    HousingInsideShop = 0x06,
    HousingOutsideShop = 0x07,
    HousingVaultHookpoint = 0x08,
    HousingCraftingHookpoint = 0x09,
    HousingBindstoneHookpoint = 0x0A,
    HousingInsideMenu = 0x0B,
    HousingTicket = 0x0C,
    HousingGuildTicket = 0x0D,
}