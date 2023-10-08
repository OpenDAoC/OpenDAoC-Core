namespace DOL.GS;

/// <summary>
/// This enumeration holds all slots that can wear attackable armor
/// </summary>
public enum EArmorSlot : int
{
	NOTSET = 0x00,
	HEAD = eInventorySlot.HeadArmor,
	HAND = eInventorySlot.HandsArmor,
	FEET = eInventorySlot.FeetArmor,
	TORSO = eInventorySlot.TorsoArmor,
	LEGS = eInventorySlot.LegsArmor,
	ARMS = eInventorySlot.ArmsArmor,
}