namespace Core.GS.PacketHandler;

public enum EPreActionType : byte
{
    UpdateLastOpened = 0,
    InitPaperdoll = 1,
    InitBackpack = 2,
    InitVaultKeeper = 3,
    InitHouseVault = 4,
    InitOwnConsigmentMerchant = 5, // have SetPrice,Withdraw
    InitConsigmentMerchant = 6,// have Buy
    HorseBags = 7,
    ContinueBackpack = 12,
    ContinueVaultKeeper = 13,
    ContinueHouseVault = 14,
    ContinueConsigmentMerchant = 15,
    ContinueOtherConsigmentMerchant = 16,
}