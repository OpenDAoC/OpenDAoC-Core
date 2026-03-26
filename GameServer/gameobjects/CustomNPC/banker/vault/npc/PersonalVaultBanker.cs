using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS
{
    public abstract class PersonalVaultBanker : VaultBanker
    {
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = $"Vault Banker {Index + 1}";
        }

        protected override string GetMiddleInteractionMessage()
        {
            return $"If your house had {ToCardinalWord(Index)} or more vaults and has been repossessed, " +
                $"I have the ability to give you the items you had in the {ToOrdinalWord(Index)} vault. ";
        }

        protected override bool TryGetHouseVault(GamePlayer player, out GameHouseVault vault)
        {
            // Do not use HouseMgr.GetHouseByPlayer because this is an account-wide check.
            vault = null;
            House house = HouseMgr.GetHouseByCharacterIds([player.ObjectId]);
            return house != null && house.HouseVaults.TryGetValue(Index, out vault);
        }

        protected override bool SetPlayerActiveInventoryObject(GamePlayer player)
        {
            // If the player has a house, it's very important not to create a different view as this can cause items to be duplicated.
            // We could give access to the house vault from here if we wanted.
            if (TryGetHouseVault(player, out _))
                return false;

            // Re-use the currently cached vault if possible.
            if (player.ActiveInventoryObject is PersonalRecoveredHouseVault cachedVault && cachedVault.Index == Index)
                return true;

            player.ActiveInventoryObject = new PersonalRecoveredHouseVault(player, AccountVaultKeeper.GetDummyVaultItem(player), Index);
            return true;
        }
    }

    public class VaultBanker1 : PersonalVaultBanker
    {
        protected override int Index => 0;
    }

    public class VaultBanker2 : PersonalVaultBanker
    {
        protected override int Index => 1;
    }

    public class VaultBanker3 : PersonalVaultBanker
    {
        protected override int Index => 2;
    }

    public class VaultBanker4 : PersonalVaultBanker
    {
        protected override int Index => 3;
    }

    public class VaultBanker5 : PersonalVaultBanker
    {
        protected override int Index => 4;
    }

    public class VaultBanker6 : PersonalVaultBanker
    {
        protected override int Index => 5;
    }

    public class VaultBanker7 : PersonalVaultBanker
    {
        protected override int Index => 6;
    }

    public class VaultBanker8 : PersonalVaultBanker
    {
        protected override int Index => 7;
    }
}
