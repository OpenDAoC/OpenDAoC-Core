using System;
using DOL.Database;

namespace DOL.GS
{
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
}
