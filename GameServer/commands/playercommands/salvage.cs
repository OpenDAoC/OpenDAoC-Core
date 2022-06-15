 using System.Collections.Generic;
 using DOL.Database;

 namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&salvage",
		ePrivLevel.Player,
		"You can salvage an item when you are a crafter",
		"/salvage")]
	public class SalvageCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "salvage"))
				return;

			if (args.Length >= 2)
			{
				if (args[1] == "all")
				{
					int firstItem = 0, lastItem = 40;
					IList<InventoryItem> items = new List<InventoryItem>();
					foreach (var item in client.Player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
					{
						if (Salvage.IsAllowedToBeginWork(client.Player, item, true))
							items.Add(item);
					}
					
					if (items.Count > 0)
						client.Player.SalvageItemList(items);
				}
			}
			else
			{
				WorldInventoryItem item = client.Player.TargetObject as WorldInventoryItem;
				if (item == null)
					return;
				client.Player.SalvageItem(item.Item);
			}
		}
	}
}