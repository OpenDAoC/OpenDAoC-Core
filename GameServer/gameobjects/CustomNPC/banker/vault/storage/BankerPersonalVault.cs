using DOL.Database;

namespace DOL.GS
{
    public class BankerPersonalVault : BankerVaultBase
    {
        public BankerPersonalVault(GamePlayer player, DbItemTemplate itemTemplate, int vaultIndex) : base(player, itemTemplate, vaultIndex) { }

        public override string GetOwner(GamePlayer player)
        {
            return $"{player.ObjectId}";
        }
    }
}
