using System.Linq;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&sell",
		ePrivLevel.Player,
		"Sell items to a targeted merchant.  Specify a single bag, a range or all",
		"Use: /sell 4 to sell all items bag 4",
		"/sell 2-3 to sell all items in bags 2 and 3",
		"/sell all to sell all items")]
	public class SellCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
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
			}

			if (client.Player is GamePlayer player && player.Inventory != null && args.Length >= 2)
            {
	            if (player.TargetObject is GameMerchant merchant)
                {
					firstItem += (int)eInventorySlot.FirstBackpack - 1;
					lastItem += (int)eInventorySlot.FirstBackpack - 1;

					var skipPotions = args.Contains("nopot");

					for (int i = firstItem; i <= lastItem; i++)
                    {
						var item = player.Inventory.GetItem((eInventorySlot)i);

						if (item != null)
						{
							if (item is {PackageID: "AtlasXPItem"} or {PackageID: "atlas_orbs_item"} or {PackageID: "atlas_potion"}) continue;
							if (skipPotions && item.Object_Type == 41) continue;
						}
						merchant.OnPlayerSell(player, item);
                    }
				}
	            else if (player.TargetObject is GameGuardMerchant guardMerchant)
	            {
		            firstItem += (int)eInventorySlot.FirstBackpack - 1;
		            lastItem += (int)eInventorySlot.FirstBackpack - 1;

		            var skipPotions = args.Contains("nopot");

		            for (int i = firstItem; i <= lastItem; i++)
		            {
			            var item = player.Inventory.GetItem((eInventorySlot)i);

			            if (item != null)
			            {
				            if (item is {PackageID: "AtlasXPItem"} or {PackageID: "atlas_orbs_item"} or {PackageID: "atlas_potion"}) continue;
				            if (skipPotions && item.Object_Type == 41) continue;
			            }
			            guardMerchant.OnPlayerSell(player, item);
		            }
	            }
				else
					client.Out.SendMessage("You must target a merchant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else
			{
				client.Out.SendMessage("Use: /sell <bag>, /sell <bag1-bag2>, /sell all", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}