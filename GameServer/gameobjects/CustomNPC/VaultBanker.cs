using System;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public abstract class VaultBankerBase : GameNPC
    {
        protected abstract int Index { get; }

        private static string GetStartInteractionMessage(GamePlayer player)
        {
            return $"Why hello {player.Name}. ";
        }

        protected abstract string GetMiddleInteractionMessage();

        private static string GetEndInteractionMessage()
        {
            return "I can't take any new possessions, but feel free to take back whatever you want.";
        }

        private string BuildInteractionMessage(GamePlayer player)
        {
            return $"{GetStartInteractionMessage(player)}{GetMiddleInteractionMessage()}{GetEndInteractionMessage()}";
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.Out.SendMessage(BuildInteractionMessage(player), eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            if (SetPlayerActiveInventoryObject(player))
                player.Out.SendInventoryItemsUpdate(player.ActiveInventoryObject.GetClientInventory(player), eInventoryWindowType.HouseVault);

            return true;
        }

        protected abstract bool TryGetHouseVault(GamePlayer player, out GameHouseVault vault);

        protected abstract bool SetPlayerActiveInventoryObject(GamePlayer player);

        protected static string ToOrdinalWord(int index)
        {
            return index switch
            {
                0 => "first",
                1 => "second",
                2 => "third",
                3 => "fourth",
                4 => "fifth",
                5 => "sixth",
                6 => "seventh",
                7 => "eighth",
                8 => "ninth",
                _ => index.ToString()
            };
        }

        protected static string ToCardinalWord(int index)
        {
            return index switch
            {
                0 => "one",
                1 => "two",
                2 => "three",
                3 => "four",
                4 => "five",
                5 => "six",
                6 => "seven",
                7 => "eight",
                8 => "nine",
                _ => index.ToString()
            };
        }
    }

    public abstract class PersonalVaultBanker : VaultBankerBase
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
            if (player.ActiveInventoryObject is BankerPersonalVault cachedVault && cachedVault.Index == Index)
                return true;

            player.ActiveInventoryObject = new BankerPersonalVault(player, AccountVaultKeeper.GetDummyVaultItem(player), Index);
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

    public abstract class GuildVaultBanker : VaultBankerBase
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

    public abstract class BankerVaultBase : GameHouseVault
    {
        protected BankerVaultBase(GamePlayer player, DbItemTemplate itemTemplate, int vaultIndex) : base(itemTemplate, vaultIndex)
        {
            if (vaultIndex is < 0)
                throw new ArgumentOutOfRangeException(nameof(vaultIndex), $"{nameof(vaultIndex)} must not be negative.");

            DbHouse dbHouse = new()
            {
                AllowAdd = false,
                GuildHouse = false,
                HouseNumber = player.ObjectID,
                Name = "Vault",
                OwnerID = GetOwner(player),
                RegionID = player.CurrentRegionID
            };

            CurrentHouse = new(dbHouse);
        }

        public override bool CanAddItems(GamePlayer player)
        {
            return false;
        }
    }

    public class BankerPersonalVault : BankerVaultBase
    {
        public BankerPersonalVault(GamePlayer player, DbItemTemplate itemTemplate, int vaultIndex) : base(player, itemTemplate, vaultIndex) { }

        public override string GetOwner(GamePlayer player)
        {
            return $"{player.ObjectId}";
        }
    }

    public class BankerGuildVault : BankerVaultBase
    {
        public BankerGuildVault(GamePlayer player, DbItemTemplate dummyTemplate, int vaultIndex) : base(player, dummyTemplate, vaultIndex) { }

        public override string GetOwner(GamePlayer player)
        {
            Guild guild = player.Guild;
            return guild == null ? string.Empty : $"{guild.GuildID}";
        }
    }
}
