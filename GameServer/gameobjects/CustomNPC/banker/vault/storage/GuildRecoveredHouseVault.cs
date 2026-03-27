using DOL.Database;

namespace DOL.GS
{
    public class GuildRecoveredHouseVault : RecoveredHouseVault
    {
        public GuildRecoveredHouseVault(GamePlayer player, DbItemTemplate dummyTemplate, int vaultIndex) : base(player, dummyTemplate, vaultIndex) { }

        protected override string BuildOwnerId(GamePlayer player)
        {
            Guild guild = player.Guild;
            return guild == null ? string.Empty : $"{guild.GuildID}";
        }
    }
}
