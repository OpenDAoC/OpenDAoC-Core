using DOL.GS.Housing;

namespace DOL.GS
{
    public class RecoveredConsignmentMerchant : GameConsignmentMerchant
    {
        public override House CurrentHouse { get; set; }

        public override bool CanHandleMove(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot)
        {
            // Allow withdrawals only.
            // This relies on GameConsignmentMerchant.MoveItem to correctly prevent item swaps between it and the player's inventory.
            return player.ActiveInventoryObject == this && this.CanHandleSlot(fromClientSlot) && !this.CanHandleSlot(toClientSlot);
        }
    }
}
