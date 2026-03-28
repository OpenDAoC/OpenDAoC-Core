using DOL.Database;

namespace DOL.GS
{
    public abstract class PersonalVaultBanker : VaultBanker
    {
        protected override VaultType Type => VaultType.Personal;

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (Index >= 0)
                GuildName = $"Vault Banker {Index + 1}";
        }
    }

    public class PersonalVaultBanker1 : PersonalVaultBanker { protected override int Index => 0; }
    public class PersonalVaultBanker2 : PersonalVaultBanker { protected override int Index => 1; }
    public class PersonalVaultBanker3 : PersonalVaultBanker { protected override int Index => 2; }
    public class PersonalVaultBanker4 : PersonalVaultBanker { protected override int Index => 3; }
    public class PersonalVaultBanker5 : PersonalVaultBanker { protected override int Index => 4; }
    public class PersonalVaultBanker6 : PersonalVaultBanker { protected override int Index => 5; }
    public class PersonalVaultBanker7 : PersonalVaultBanker { protected override int Index => 6; }
    public class PersonalVaultBanker8 : PersonalVaultBanker { protected override int Index => 7; }
}
