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
			int firstItem = 0, lastItem = 0, firstBag = 0, lastBag = 0;


			if (args.Length >= 2)
			{
				if (args[1].Contains("all"))
				{
					firstItem = 1;
					lastItem = 40;
				}
				else if (args[1].Contains('-'))
				{ 
					string [] bags = args[1].Split("-".ToCharArray(), 2);
					firstBag = int.TryParse(bags[0], out firstBag) ? firstBag : 0;
					lastBag = int.TryParse(bags[1], out lastBag) ? lastBag : 0;
					
					// if (firstBag > lastBag)
					// {
					// 	(firstBag, lastBag) = (lastBag, firstBag);
					// }

					switch(firstBag)
					{
						case 1:
							firstItem = 1;
							break;
						case 2:
							firstItem = 9;
							break;
						case 3:
							firstItem = 17;
							break;
						case 4:
							firstItem = 25;
							break;
						case 5:
							firstItem = 33;
							break;
					}

					switch (lastBag)
					{
						case 1:
							lastItem = 8;
							break;
						case 2:
							lastItem = 16;
							break;
						case 3:
							lastItem = 24;
							break;
						case 4:
							lastItem = 32;
							break;
						case 5:
							lastItem = 40;
							break;
					}
					
				} 
				else if (int.TryParse(args[1], out int bag))
				{
					switch (bag)
					{
						case 1:
							firstItem = 1;
							lastItem = 8;
							break;
						case 2:
							firstItem = 9;
							lastItem = 16;
							break;
						case 3:
							firstItem = 17;
							lastItem = 24;
							break;
						case 4:
							firstItem = 25;
							lastItem = 32;
							break;
						case 5:
							firstItem = 33;
							lastItem = 40;
							break;
					}
				}
				
				IList<InventoryItem> items = new List<InventoryItem>();
				
				firstItem += (int)eInventorySlot.FirstBackpack - 1;
				lastItem += (int)eInventorySlot.FirstBackpack - 1;
				
				for (int i = firstItem; i <= lastItem; i++)
				{
					var item = client.Player.Inventory.GetItem((eInventorySlot)i);

					if (item == null) continue;
					if (Salvage.IsAllowedToBeginWork(client.Player, item, true))
						items.Add(item);
				}

				if (items.Count > 0)
					client.Player.SalvageItemList(items);
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