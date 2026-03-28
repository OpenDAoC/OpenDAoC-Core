using DOL.Database;

namespace DOL.GS
{
    public class PersonalConsignmentBanker : ConsignmentBanker
    {
        protected override VaultType? BankerType => VaultType.Personal;

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = "Personal Consignment Banker";
        }
    }
}
