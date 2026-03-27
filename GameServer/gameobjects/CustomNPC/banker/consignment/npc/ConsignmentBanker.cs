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

            // House.GetPermissionLevel will currently return null, and only the guild leader will be able to interact with the banker.
            if (TryGetConsignmentMerchant(player, out GameConsignmentMerchant consignmentMerchant))
                consignmentMerchant.Interact(player);

            return true;
        }

        protected abstract bool TryGetConsignmentMerchant(GamePlayer player, out GameConsignmentMerchant consignmentMerchant);

        protected static RecoveredConsignmentMerchant CreateDummyConsignmentMerchant(GamePlayer player, House house)
        {
            // For basic withdrawal, only CurrentHouse and a valid position should be needed.
            // HouseNumber will be non 0 only if the player or guild owns a house but no consignment merchant.
            RecoveredConsignmentMerchant consignmentMerchant = new()
            {
                CurrentHouse = house,
                HouseNumber = (ushort) house.HouseNumber,
                CurrentRegion = player.CurrentRegion,
                X = player.X,
                Y = player.Y,
                Z = player.Z
            };

            consignmentMerchant.movementComponent.ForceUpdatePosition();

            // Load money if any.
            // TotalMoney's setter performs a DB query and save. This should be changed.
            DbHouseConsignmentMerchant houseCm = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(house.OwnerID));

            if (houseCm != null && houseCm.Money > 0)
                consignmentMerchant.TotalMoney = houseCm.Money;

            return consignmentMerchant;
        }

        protected abstract House CreateDummyHouse(string ownerId);
    }
}
