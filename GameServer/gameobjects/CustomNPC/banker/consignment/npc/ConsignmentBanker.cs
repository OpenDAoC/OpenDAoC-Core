using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public abstract class ConsignmentBanker : GameNPC
    {
        private static string BuildInteractionMessage(GamePlayer player)
        {
            return $"You will be able to retrieve any items that your consignment merchant would have had if you had one and your house was repossessed.";
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.Out.SendMessage(BuildInteractionMessage(player), eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            if (TryGetConsignmentMerchant(player, out GameConsignmentMerchant consignmentMerchant))
                consignmentMerchant.Interact(player);

            return true;
        }

        protected abstract bool TryGetConsignmentMerchant(GamePlayer player, out GameConsignmentMerchant consignmentMerchant);

        protected static RecoveredConsignmentMerchant CreateDummyConsignmentMerchant(House house)
        {
            // For basic withdrawal, only CurrentHouse should be needed.
            // HouseNumber will be non 0 only if the player or guild owns a house but no consignment merchant.
            RecoveredConsignmentMerchant consignmentMerchant = new()
            {
                CurrentHouse = house,
                HouseNumber = (ushort) house.HouseNumber
            };

            DbHouseConsignmentMerchant houseCm = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(house.OwnerID));

            if (houseCm != null)
                consignmentMerchant.TotalMoney = houseCm.Money;

            return consignmentMerchant;
        }

        protected static House CreateDummyHouse(string ownerId)
        {
            return new(new()
            {
                OwnerID = ownerId
            });
        }
    }
}
