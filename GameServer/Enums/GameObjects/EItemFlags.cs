namespace Core.GS.Enums;

public enum EItemFlags
{
	CannotBeSoldToMerchants = 0x1,
	CannotBeDestroyed = 0x2,
	CannotBeTradedToOtherPlayers = 0x4,
	CannotBeDropped = 0x8,
	CanBeDroppedAsLoot = 0x10,
}