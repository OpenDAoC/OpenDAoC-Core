using DOL.GS.Housing;

namespace DOL.GS
{
    public class RecoveredConsignmentMerchant : GameConsignmentMerchant
    {
        public override House CurrentHouse { get; set; } // Base class relies on HouseNumber, which may be 0.

        public RecoveredConsignmentMerchant(GamePlayer player, House house)
        {
            CurrentHouse = house;
            HouseNumber = (ushort) house.HouseNumber;

            // Allows interaction.
            CurrentRegion = player.CurrentRegion;
            X = player.X;
            Y = player.Y;
            Z = player.Z;
            movementComponent.ForceUpdatePosition();
        }

        public override bool CanHandleMove(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot)
        {
            // Allow withdrawals only.
            return player.ActiveInventoryObject == this && this.CanHandleSlot(fromClientSlot) && !this.CanHandleSlot(toClientSlot);
        }
    }
}
