using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public abstract class VaultBanker : GameNPC
    {
        protected virtual int Index => -1; // Legacy single-vault bankers override these. Unified banker defaults to -1.
        protected virtual VaultType Type => VaultType.Personal;

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            // Execute default behavior only for legacy single-index bankers.
            if (Index >= 0)
            {
                player.Out.SendMessage(BuildInteractionMessage(player, Type, Index), eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                if (TryGetHouseVault(player, Type, Index, out GameHouseVault houseVault))
                    houseVault.Interact(player);
            }

            return true;
        }

        protected static string BuildInteractionMessage(GamePlayer player, VaultType type, int index)
        {
            string msg = $"Why hello {player.Name}. ";

            if (type is VaultType.Personal)
            {
                msg +=
                    $"If your house had {ToCardinalWord(index)} or more vaults and has been repossessed, " +
                    $"I have the ability to give you the items you had in the {ToOrdinalWord(index)} vault. ";
            }
            else if (type is VaultType.Guild)
            {
                msg +=
                    $"If your guild's house has been repossessed, " +
                    $"and you had permission to use the guild vaults, " +
                    $"I can give you the items from the {ToOrdinalWord(index)} vault. ";
            }
            else
                return msg;

            msg += "I can't take any new possessions, but feel free to take back whatever you want.";
            return msg;
        }

        protected static bool TryGetHouseVault(GamePlayer player, VaultType vaultType, int index, out GameHouseVault vault)
        {
            vault = null;

            if (TryGetRealHouseVault(player, vaultType, index, out _))
                return false;

            if (vaultType is VaultType.Personal)
            {
                if (player.ActiveInventoryObject is PersonalRecoveredHouseVault cachedVault && cachedVault.Index == index)
                {
                    vault = cachedVault;
                    return true;
                }

                vault = new PersonalRecoveredHouseVault(player, AccountVaultKeeper.GetDummyVaultItem(player), index)
                {
                    CurrentHouse = new NullHouse(player.ObjectId, false)
                };

                return true;
            }

            if (vaultType is VaultType.Guild)
            {
                if (player.ActiveInventoryObject is GuildRecoveredHouseVault cachedVault && cachedVault.Index == index)
                {
                    vault = cachedVault;
                    return true;
                }

                Guild guild = player.Guild;

                if (guild == null)
                    return false;

                vault = new GuildRecoveredHouseVault(player, AccountVaultKeeper.GetDummyVaultItem(player), index)
                {
                    CurrentHouse = new NullHouse(guild.GuildID, true)
                };

                return true;
            }

            return false;
        }

        private static bool TryGetRealHouseVault(GamePlayer player, VaultType type, int index, out GameHouseVault vault)
        {
            vault = null;
            House house = null;

            if (type is VaultType.Personal)
                house = HouseMgr.GetHouseByCharacterIds([player.ObjectId]);
            else if (type is VaultType.Guild)
                house = HouseMgr.GetGuildHouseByPlayer(player);

            return house != null && house.HouseVaults.TryGetValue(index, out vault);
        }

        private static string ToOrdinalWord(int index)
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
                _ => index.ToString()
            };
        }

        private static string ToCardinalWord(int index)
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
                _ => index.ToString()
            };
        }
    }
}
