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
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&sell",
		ePrivLevel.Player,
		"Sell items to a targeted merchant.  Specify a single bag or all",
		"Use: /sell 4 to sell all items in the 4th bag","/sell all to sell all items")]
	public class SellCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			int firstItem = 0, lastItem = 0;

			switch (args.Length)
            {
				case 2:
					
					switch (args[1])
					{
						case "1":
							firstItem = 1;
							lastItem = 8;
							break;
						case "2":
							firstItem = 9;
							lastItem = 16;
							break;
						case "3":
							firstItem = 17;
							lastItem = 24;
							break;
						case "4":
							firstItem = 25;
							lastItem = 32;
							break;
						case "5":
							firstItem = 33;
							lastItem = 40;
							break;
						case "all":
							firstItem = 1;
							lastItem = 40;
							break;
						default:
							client.Out.SendMessage("Use: /sell <bag> or /sell all", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
					}
					break;
				default:
					client.Out.SendMessage("Use: /sell <bag> or /sell all", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
			}
			
			if (client.Player is GamePlayer player && player.Inventory != null)
            {
				if (player.TargetObject is GameMerchant merchant)
                {
					firstItem += (int)eInventorySlot.FirstBackpack - 1;
					lastItem += (int)eInventorySlot.FirstBackpack - 1;

					for (int i = firstItem; i <= lastItem; i++)
                    {
						InventoryItem item = player.Inventory.GetItem((eInventorySlot)i);
						if (item != null)
							merchant.OnPlayerSell(player, item);
                    }
				}
				else
					client.Out.SendMessage("You must target a merchant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}