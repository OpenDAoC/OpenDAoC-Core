using DOL.Database;

namespace DOL.GS
{
    public class GuildConsignmentBanker : ConsignmentBanker
    {
        protected override VaultType? BankerType => VaultType.Guild;

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = "Guild Consignment Banker";
        }
    }
}
