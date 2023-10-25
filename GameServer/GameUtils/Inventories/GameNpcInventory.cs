using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.GameUtils;

/// <summary>
/// A class for individual NPC inventories
/// this bypasses shared inventory templates which we sometimes need
/// </summary>
public class GameNpcInventory : GameLivingInventory
{
	/// <summary>
	/// Creates a Guard Inventory from an Inventory Template
	/// </summary>
	/// <param name="template"></param>
	public GameNpcInventory(GameNpcInventoryTemplate template)
	{
		foreach (DbInventoryItem item in template.AllItems)
		{
			AddItem((EInventorySlot)item.SlotPosition, GameInventoryItem.Create(item));
		}
	}
}