 /*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

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
						items.Add(item);
					}

					// for (int i = firstItem; i <= lastItem; i++)
					// {
					// 	InventoryItem item = client.Player.Inventory.GetItem((eInventorySlot)i);
					// 	if (item != null)
					// 		merchant.OnPlayerSell(player, item);
					// }
					// client.Player.SalvageAllItems();
					if (items.Count > 0)
						client.Player.SalvageItemList(items);
					return;
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