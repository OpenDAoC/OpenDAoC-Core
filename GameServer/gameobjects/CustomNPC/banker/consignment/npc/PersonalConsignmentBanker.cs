using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS
{
    public class PersonalConsignmentBanker : ConsignmentBanker
    {
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = $"Personal Consignment Banker";
        }

        protected override bool TryGetConsignmentMerchant(GamePlayer player, out GameConsignmentMerchant consignmentMerchant)
        {
            consignmentMerchant = null;
            House house = HouseMgr.GetHouseByCharacterIds([player.ObjectId]);

            // We could give access to the consignment merchant from here if we wanted.
            if (house == null)
                house = CreateDummyHouse(player.ObjectId);
            else if (house.ConsignmentMerchant != null)
                return false;

            consignmentMerchant = CreateDummyConsignmentMerchant(house);
            return true;
        }
    }
}
