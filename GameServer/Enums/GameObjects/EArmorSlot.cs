namespace Core.GS.Enums;

/// <summary>
/// This enumeration holds all slots that can wear attackable armor
/// </summary>
public enum EArmorSlot : int
{
	NOTSET = 0x00,
	HEAD = EInventorySlot.HeadArmor,
	HAND = EInventorySlot.HandsArmor,
	FEET = EInventorySlot.FeetArmor,
	TORSO = EInventorySlot.TorsoArmor,
	LEGS = EInventorySlot.LegsArmor,
	ARMS = EInventorySlot.ArmsArmor,
}