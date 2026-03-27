using System;
using DOL.Database;

namespace DOL.GS
{
    public abstract class RecoveredHouseVault : GameHouseVault
    {
        private string _vaultOwner;

        protected RecoveredHouseVault(GamePlayer player, DbItemTemplate itemTemplate, int vaultIndex) : base(itemTemplate, vaultIndex)
        {
            if (vaultIndex is < 0)
                throw new ArgumentOutOfRangeException(nameof(vaultIndex), $"{nameof(vaultIndex)} must not be negative.");

            _vaultOwner = BuildOwnerId(player);

            DbHouse dbHouse = new()
            {
                AllowAdd = false,
                GuildHouse = false,
                HouseNumber = player.ObjectID,
                Name = "Vault",
                OwnerID = _vaultOwner,
                RegionID = player.CurrentRegionID
            };

            CurrentHouse = new(dbHouse);
        }

        public override bool CanAddItems(GamePlayer player)
        {
            return false;
        }

        protected abstract string BuildOwnerId(GamePlayer player);
    }
}
