using DOL.Database;

namespace DOL.GS
{
    public class GuildRecoveredHouseVault : RecoveredHouseVault
    {
        public GuildRecoveredHouseVault(GamePlayer player, DbItemTemplate dummyTemplate, int vaultIndex) : base(player, dummyTemplate, vaultIndex) { }

        public override string GetOwner(GamePlayer player)
        {
            Guild guild = player.Guild;
            return guild == null ? string.Empty : $"{guild.GuildID}";
        }
    }
}
