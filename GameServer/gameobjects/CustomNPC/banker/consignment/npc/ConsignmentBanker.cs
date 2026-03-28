using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public abstract class ConsignmentBanker : GameNPC
    {
        protected virtual VaultType? BankerType => null; // Legacy single-type bankers override this. Master banker returns null.

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            // Execute default behavior only for legacy single-type bankers.
            if (BankerType.HasValue)
            {
                player.Out.SendMessage(BuildInteractionMessage(), eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                // House.GetPermissionLevel will currently return null, and only the guild leader will be able to interact with the banker.
                if (TryGetConsignmentMerchant(player, BankerType.Value, out GameConsignmentMerchant consignmentMerchant))
                    consignmentMerchant.Interact(player);
            }

            return true;
        }

        private static string BuildInteractionMessage()
        {
            return $"You will be able to retrieve any items that your consignment merchant would have had if you had one and your house was repossessed.";
        }

        protected static bool TryGetConsignmentMerchant(GamePlayer player, VaultType type, out GameConsignmentMerchant consignmentMerchant)
        {
            consignmentMerchant = null;

            if (type is VaultType.Personal)
            {
                House house = HouseMgr.GetHouseByCharacterIds([player.ObjectId]);

                // We could give access to the consignment merchant from here if we wanted.
                if (house == null)
                    house = CreateDummyHouse(player.ObjectId, type);
                else if (house.ConsignmentMerchant != null)
                    return false;

                consignmentMerchant = CreateDummyConsignmentMerchant(player, house);
                return true;
            }

            if (type is VaultType.Guild)
            {
                Guild guild = player.Guild;

                if (guild == null)
                    return false;

                House house = HouseMgr.GetGuildHouseByPlayer(player);

                // We could give access to the consignment merchant from here if we wanted.
                if (house == null)
                    house = CreateDummyHouse(guild.GuildID, type);
                else if (house.ConsignmentMerchant != null)
                    return false;

                consignmentMerchant = CreateDummyConsignmentMerchant(player, house);
                return true;
            }

            return false;
        }

        private static RecoveredConsignmentMerchant CreateDummyConsignmentMerchant(GamePlayer player, House house)
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
            return consignmentMerchant;
        }

        private static House CreateDummyHouse(string ownerId, VaultType type)
        {
            return new(new()
            {
                OwnerID = ownerId,
                GuildHouse = type is VaultType.Guild
            });
        }
    }
}
