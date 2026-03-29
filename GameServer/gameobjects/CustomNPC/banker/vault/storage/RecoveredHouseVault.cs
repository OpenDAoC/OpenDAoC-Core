using System;
using DOL.Database;

namespace DOL.GS
{
    public abstract class RecoveredHouseVault : GameHouseVault
    {
        protected RecoveredHouseVault(GamePlayer player, DbItemTemplate itemTemplate, int vaultIndex) : base(itemTemplate, vaultIndex)
        {
            if (vaultIndex is < 0)
                throw new ArgumentOutOfRangeException(nameof(vaultIndex), $"{nameof(vaultIndex)} must not be negative.");

            // Allows interaction.
            X = player.X;
            Y = player.Y;
            Z = player.Z;
            CurrentRegion = player.CurrentRegion;
        }

        public override bool CanAddItems(GamePlayer player)
        {
            return false;
        }

        protected abstract string BuildOwnerId(GamePlayer player);
    }
}
