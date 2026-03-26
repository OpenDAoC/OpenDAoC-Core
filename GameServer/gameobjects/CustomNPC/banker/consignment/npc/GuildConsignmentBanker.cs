using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS
{
    public class GuildConsignmentBanker : ConsignmentBanker
    {
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = $"Guild Consignment Banker";
        }

        protected override bool TryGetConsignmentMerchant(GamePlayer player, out GameConsignmentMerchant consignmentMerchant)
        {
            consignmentMerchant = null;
            Guild guild = player.Guild;

            if (guild == null)
                return false;

            House house = HouseMgr.GetGuildHouseByPlayer(player);

            // We could give access to the consignment merchant from here if we wanted.
            if (house == null)
                house = CreateDummyHouse(guild.GuildID);
            else if (house.ConsignmentMerchant != null)
                return false;

            consignmentMerchant = CreateDummyConsignmentMerchant(house);
            return true;
        }
    }
}
