using System;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&craft",
		ePrivLevel.Player,
		"Crafting macros and utilities",
		"'/craft set <#>' to set how many items you want to craft",
		"'/craft buy' to buy the necessary materials to craft one item",
		"'/craft buy <#>' to buy the necessary materials to craft <#> item",
		"'/craft clear' to reset to crafting once",
		"'/craft show' to show the current craft settings")]
	public class CraftMacroCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public const string CraftQueueLength = "CraftQueueLength";
		public const string RecipeToCraft = "RecipeToCraft";
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length >= 2)
			{
				if (args[1] == "set")
				{
					if (args.Length >= 3)
					{
						int.TryParse(args[2], out int count);
						if (count == 0)
						{
							client.Out.SendMessage("Use: /craft set <#>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						client.Player.TempProperties.setProperty(CraftQueueLength, count);
						client.Out.SendMessage($"Crafting queue set to {count} items", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else
					{
						client.Out.SendMessage("Use: /craft set <#>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
				}

				if (args[1].Contains("clear"))
				{
					if (client.Player.TempProperties.getProperty<int>(CraftQueueLength) != 0)
					{
						client.Player.TempProperties.removeProperty(CraftQueueLength);
						client.Out.SendMessage("Crafting queue reset to 1", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else
						client.Out.SendMessage("The crafting queue is already set to 1", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					var recipe = client.Player.TempProperties.getProperty<Recipe>(RecipeToCraft);
					if (recipe != null)
					{
						client.Player.TempProperties.removeProperty(RecipeToCraft);
						client.Out.SendMessage("Crafting buying macro item cleared", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
				}
				
				if (args[1].Contains("show"))
				{
					if (client.Player.TempProperties.getProperty<int>(CraftQueueLength) != 0)
						client.Out.SendMessage($"Crafting queue set to {client.Player.TempProperties.getProperty<int>(CraftQueueLength)}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
						client.Out.SendMessage("Crafting queue set to 1", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				if (args[1].Contains("buy"))
				{
					int amount = 1;
					if (args.Length >= 3)
					{
						int.TryParse(args[2], out amount);
						if (amount == 0)
						{
							amount = 1;
						}
					}
					
					var recipe = client.Player.TempProperties.getProperty<Recipe>("RecipeToCraft");
					if (recipe != null)
					{
						if (client.Player.TargetObject is GameMerchant merchant)
						{
							client.Out.SendMessage($"Buying items to craft {amount}x {recipe.Product.Name}", eChatType.CT_System, eChatLoc.CL_SystemWindow);

							var merchantitems = DOLDB<MerchantItem>.SelectObjects(DB.Column("ItemListID").IsEqualTo(merchant.TradeItems.ItemsListID));

							foreach (var ingredient in recipe.Ingredients)
							{
								foreach (var items in merchantitems)
								{
									ItemTemplate item = GameServer.Database.FindObjectByKey<ItemTemplate>(items.ItemTemplateID);
									if (item != ingredient.Material) continue;
									merchant.OnPlayerBuy(client.Player, items.SlotPosition, items.PageNumber, ingredient.Count * amount);
								}
							}
							client.Out.SendMessage($"Bought items to craft {amount}x {recipe.Product.Name}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						
						client.Out.SendMessage("You must target a merchant", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						return;
						
					}
					client.Out.SendMessage("No recipe selected. Start crafting an item first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
			else
			{
				client.Out.SendMessage("Use: `/craft set <#>', `/craft clear', `/craft show'", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}