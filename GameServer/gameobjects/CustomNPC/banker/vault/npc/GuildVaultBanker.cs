using DOL.Database;

namespace DOL.GS
{
    public abstract class GuildVaultBanker : VaultBanker
    {
        protected override VaultType Type => VaultType.Guild;

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (Index >= 0)
                GuildName = $"Guild Vault Banker {Index + 1}";
        }
    }

    public class GuildVaultBanker1 : GuildVaultBanker { protected override int Index => 0; }
    public class GuildVaultBanker2 : GuildVaultBanker { protected override int Index => 1; }
    public class GuildVaultBanker3 : GuildVaultBanker { protected override int Index => 2; }
    public class GuildVaultBanker4 : GuildVaultBanker { protected override int Index => 3; }
    public class GuildVaultBanker5 : GuildVaultBanker { protected override int Index => 4; }
    public class GuildVaultBanker6 : GuildVaultBanker { protected override int Index => 5; }
    public class GuildVaultBanker7 : GuildVaultBanker { protected override int Index => 6; }
    public class GuildVaultBanker8 : GuildVaultBanker { protected override int Index => 7; }
}
