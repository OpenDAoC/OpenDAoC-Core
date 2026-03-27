using DOL.Database;

namespace DOL.GS
{
    public class PersonalRecoveredHouseVault : RecoveredHouseVault
    {
        public PersonalRecoveredHouseVault(GamePlayer player, DbItemTemplate itemTemplate, int vaultIndex) : base(player, itemTemplate, vaultIndex) { }

        protected override string BuildOwnerId(GamePlayer player)
        {
            return $"{player.ObjectId}";
        }
    }
}
