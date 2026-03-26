using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS
{
    public abstract class GuildVaultBanker : VaultBanker
    {
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = $"Guild Vault Banker {Index + 1}";
        }

        protected override string GetMiddleInteractionMessage()
        {
            return $"If your guild's house has been repossessed, " +
                $"and you had permission to use the guild vaults, " +
                $"I can give you the items from the {ToOrdinalWord(Index)} vault. ";
        }

        protected override bool TryGetHouseVault(GamePlayer player, out GameHouseVault vault)
        {
            vault = null;
            House house = HouseMgr.GetGuildHouseByPlayer(player);
            return house != null && house.HouseVaults.TryGetValue(Index, out vault);
        }

        protected override bool SetPlayerActiveInventoryObject(GamePlayer player)
        {
            // If the player's guild has a house, it's very important not to create a different view as this can cause items to be duplicated.
            // We could give access to the house vault from here if we wanted.
            if (TryGetHouseVault(player, out _))
                return false;

            // Re-use the currently cached vault if possible.
            if (player.ActiveInventoryObject is BankerGuildVault cachedVault && cachedVault.Index == Index)
                return true;

            if (player.Guild == null)
                return false;

            player.ActiveInventoryObject = new BankerGuildVault(player, AccountVaultKeeper.GetDummyVaultItem(player), Index);
            return true;
        }
    }

    public class GuildVaultBanker1 : GuildVaultBanker
    {
        protected override int Index => 0;
    }

    public class GuildVaultBanker2 : GuildVaultBanker
    {
        protected override int Index => 1;
    }

    public class GuildVaultBanker3 : GuildVaultBanker
    {
        protected override int Index => 2;
    }

    public class GuildVaultBanker4 : GuildVaultBanker
    {
        protected override int Index => 3;
    }

    public class GuildVaultBanker5 : GuildVaultBanker
    {
        protected override int Index => 4;
    }

    public class GuildVaultBanker6 : GuildVaultBanker
    {
        protected override int Index => 5;
    }

    public class GuildVaultBanker7 : GuildVaultBanker
    {
        protected override int Index => 6;
    }

    public class GuildVaultBanker8 : GuildVaultBanker
    {
        protected override int Index => 7;
    }
}
