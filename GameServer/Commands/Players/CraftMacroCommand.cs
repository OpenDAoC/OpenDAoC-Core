﻿using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Keeps;

namespace DOL.GS.Commands;

[Command(
    "&craftmacro",
    ePrivLevel.Player,
    "Crafting macros and utilities",
    "'/craftmacro set <#>' to set how many items you want to craft",
    "'/craftmacro clear' to reset to crafting once",
    "'/craftmacro show' to show the current craft settings",
    "'/craftmacro buy' to buy the necessary materials to craft one item",
    "'/craftmacro buy <#>' to buy the necessary materials to craft <#> items",
    "'/craftmacro buyto <#>' to buy only the missing materials to craft <#> items")]
public class CraftMacroCommand : ACommandHandler, ICommandHandler
{
    public const string CraftQueueLength = "CraftQueueLength";
    public const string RecipeToCraft = "RecipeToCraft";

    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length >= 2)
        {
            #region set

            if (args[1] == "set")
            {
                if (args.Length >= 3)
                {
                    int.TryParse(args[2], out int count);
                    if (count == 0)
                    {
                        DisplayMessage(client, "Use: /craft set <#>");
                        return;
                    }

                    if (count > 100)
                    {
                        count = 100;
                    }
            
                    client.Player.TempProperties.SetProperty(CraftQueueLength, count);
                    DisplayMessage(client, $"Crafting queue set to {count} items");
                }
                else
                {
                    DisplayMessage(client, "Use: /craft set <#>");
                }
            }

            #endregion

            #region clear

            if (args[1] == "clear")
            {

                client.Player.TempProperties.RemoveProperty(CraftQueueLength);
                

                var recipe = client.Player.TempProperties.GetProperty<RecipeMgr>(RecipeToCraft);
                if (recipe != null)
                {
                    client.Player.TempProperties.RemoveProperty(RecipeToCraft);
                }

                DisplayMessage(client, "Crafting queue reset to 1 and item cleared");
            }

            #endregion

            #region show

            if (args[1] == "show")
            {
                if (client.Player.TempProperties.GetProperty<int>(CraftQueueLength) != 0)
                    DisplayMessage(client,
                        $"Crafting queue set to {client.Player.TempProperties.GetProperty<int>(CraftQueueLength)}");
                else
                    DisplayMessage(client, "Crafting queue set to 1");
            }

            #endregion

            #region buy

            if (args[1] == "buy")
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

                var recipe = client.Player.TempProperties.GetProperty<RecipeMgr>("RecipeToCraft");
                if (recipe != null)
                {
                    if (client.Player.TargetObject is GameMerchant merchant)
                    {
                        var merchantitems = DOLDB<DbMerchantItem>.SelectObjects(DB.Column("ItemListID")
                            .IsEqualTo(merchant.TradeItems.ItemsListID));
                        
                        IList<IngredientDb> recipeIngredients;

                        lock (recipe)
                        {
                            recipeIngredients = recipe.Ingredients;
                        }
                        
                        foreach (var ingredient in recipeIngredients)
                        {
                            foreach (var items in merchantitems)
                            {
                                var item =
                                    GameServer.Database.FindObjectByKey<DbItemTemplate>(items.ItemTemplateID);
                                if (item.Id_nb == "beetle_carapace") continue;
                                if (item != ingredient.Material) continue;
                                merchant.OnPlayerBuy(client.Player, items.SlotPosition, items.PageNumber,
                                    ingredient.Count * amount);
                            }
                        }

                        return;
                    }
                    else if (client.Player.TargetObject is GameGuardMerchant guardMerchant)
                    {
                        var merchantitems = DOLDB<DbMerchantItem>.SelectObjects(DB.Column("ItemListID")
                            .IsEqualTo(guardMerchant.TradeItems.ItemsListID));
                        
                        IList<IngredientDb> recipeIngredients;

                        lock (recipe)
                        {
                            recipeIngredients = recipe.Ingredients;
                        }
                        
                        foreach (var ingredient in recipeIngredients)
                        {
                            foreach (var items in merchantitems)
                            {
                                var item =
                                    GameServer.Database.FindObjectByKey<DbItemTemplate>(items.ItemTemplateID);
                                if (item.Id_nb == "beetle_carapace") continue;
                                if (item != ingredient.Material) continue;
                                guardMerchant.OnPlayerBuy(client.Player, items.SlotPosition, items.PageNumber,
                                    ingredient.Count * amount);
                            }
                        }

                        return;
                    }
                    else if (client.Player.TargetObject is GuardCurrencyMerchant guardCurrencyMerchant)
                    {
                        var merchantitems = DOLDB<DbMerchantItem>.SelectObjects(DB.Column("ItemListID")
                            .IsEqualTo(guardCurrencyMerchant.TradeItems.ItemsListID));
                        
                        IList<IngredientDb> recipeIngredients;

                        lock (recipe)
                        {
                            recipeIngredients = recipe.Ingredients;
                        }
                        
                        foreach (var ingredient in recipeIngredients)
                        {
                            foreach (var items in merchantitems)
                            {
                                var item =
                                    GameServer.Database.FindObjectByKey<DbItemTemplate>(items.ItemTemplateID);
                                if (item.Id_nb == "beetle_carapace") continue;
                                if (item != ingredient.Material) continue;
                                guardCurrencyMerchant.OnPlayerBuy(client.Player, items.SlotPosition, items.PageNumber,
                                    ingredient.Count * amount);
                            }
                        }

                        return;
                    }
                    DisplayMessage(client, "You must target a merchant");
                    return;
                }

                DisplayMessage(client, "No recipe selected, start crafting an item first");
            }

            #endregion

            #region buyto

            if (args[1] == "buyto")
            {
                if (args.Length < 3)
                {
                    DisplayMessage(client, "Use: /craft buyto <#>");
                    return;
                }

                if (int.TryParse(args[2], out int amount))
                {
                    if (amount == 0)
                    {
                        amount = 1;
                    }
                }
                else
                {
                    DisplayMessage(client, "Use: /craft buyto <#>");
                    return;
                }

                var recipe = client.Player.TempProperties.GetProperty<RecipeMgr>("RecipeToCraft");
                if (recipe != null)
                {
                    if (client.Player.TargetObject is GameMerchant merchant)
                    {
                        var merchantitems = DOLDB<DbMerchantItem>.SelectObjects(DB.Column("ItemListID")
                            .IsEqualTo(merchant.TradeItems.ItemsListID));

                        IList<IngredientDb> recipeIngredients;

                        lock (recipe)
                        {
                            recipeIngredients = recipe.Ingredients;
                        }
                        
                        var playerItems = new List<DbInventoryItem>(); 
                                
                        lock (client.Player.Inventory)
                        {
                            foreach (var pItem in client.Player.Inventory.AllItems)
                            {
                                if (pItem.SlotPosition < (int)eInventorySlot.FirstBackpack ||
                                    pItem.SlotPosition > (int)eInventorySlot.LastBackpack)
                                    continue; 
                                playerItems.Add(pItem);
                            }
                        }
                        
                        foreach (var ingredient in recipeIngredients)
                        {
                            foreach (var items in merchantitems)
                            {
                                var item =
                                    GameServer.Database.FindObjectByKey<DbItemTemplate>(items.ItemTemplateID);
                                if (item.Id_nb == "beetle_carapace") continue;
                                if (item != ingredient.Material) continue;
                                int playerAmount = 0;

                                foreach (var pItem in playerItems)
                                {
                                    if (pItem.Template == ingredient.Material)
                                        playerAmount += pItem.Count;
                                }

                                merchant.OnPlayerBuy(client.Player, items.SlotPosition, items.PageNumber,
                                    (ingredient.Count * amount) - playerAmount);
                            }
                        }

                        DisplayMessage(client, $"Bought items to craft {amount}x {recipe.Product.Name}");
                        return;
                    }

                    DisplayMessage(client, "You must target a merchant");
                    return;
                }

                DisplayMessage(client, "No recipe selected, start crafting an item first");
            }

            #endregion
        }
        else
        {
            DisplaySyntax(client);
        }
    }
}