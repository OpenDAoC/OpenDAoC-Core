using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS;

public class AccountVaultKeeper : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public override bool Interact(GamePlayer player)
    {
        if (!base.Interact(player))
            return false;
        
        if (player.HCFlag)
        {
            SayTo(player,$"I'm sorry {player.Name}, my vault is not Hardcore enough for you.");
            return false;
        }

        // if (player.Level <= 1)
        // {
        //     SayTo(player,$"I'm sorry {player.Name}, come back if you are venerable to use my services.");
        //     return false;
        // }

        string message = $"Greetings {player.Name}, nice meeting you.\n";
        
        message += "I am happy to offer you my services.\n\n";

        message += "You can browse the [first] or [second] page of your Account Vault.";
        player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_PopupWindow);

        DbItemTemplate vaultItem = GetDummyVaultItem(player);
        AccountVault vault = new AccountVault(player, this, player.Client.Account.Name + "_" + player.Realm.ToString(), 0, vaultItem);
        player.ActiveInventoryObject = vault;
        player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), EInventoryWindowType.HouseVault);
        return true;
    }

    public override bool WhisperReceive(GameLiving source, string text)
    {
        if (!base.WhisperReceive(source, text))
            return false;

        GamePlayer player = source as GamePlayer;

        if (player == null)
            return false;

        if (text == "first")
        {
            AccountVault vault = new AccountVault(player, this, player.Client.Account.Name + "_" + player.Realm.ToString(), 0, GetDummyVaultItem(player));
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), EInventoryWindowType.HouseVault);
        }
        else if (text == "second")
        {
            AccountVault vault = new AccountVault(player, this, player.Client.Account.Name + "_" + player.Realm.ToString(), 1, GetDummyVaultItem(player));
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), EInventoryWindowType.HouseVault);
        }

        return true;
    }

    private static DbItemTemplate GetDummyVaultItem(GamePlayer player)
    {
        DbItemTemplate vaultItem = new DbItemTemplate();
        vaultItem.Object_Type = (int)EObjectType.HouseVault;
        vaultItem.Name = "Vault";
        vaultItem.ObjectId = player.Client.Account.Name + "_" + player.Realm.ToString();
        switch (player.Realm)
        {
            case ERealm.Albion:
                vaultItem.Id_nb = "housing_alb_vault";
                vaultItem.Model = 1489;
                break;
            case ERealm.Hibernia:
                vaultItem.Id_nb = "housing_hib_vault";
                vaultItem.Model = 1491;
                break;
            case ERealm.Midgard:
                vaultItem.Id_nb = "housing_mid_vault";
                vaultItem.Model = 1493;
                break;
        }

        return vaultItem;
    }
}