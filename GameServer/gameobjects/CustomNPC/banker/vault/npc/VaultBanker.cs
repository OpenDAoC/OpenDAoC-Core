using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public abstract class VaultBanker : GameNPC
    {
        protected abstract int Index { get; }

        protected virtual string GetStartInteractionMessage(GamePlayer player)
        {
            return $"Why hello {player.Name}. ";
        }

        protected abstract string GetMiddleInteractionMessage();

        protected virtual string GetEndInteractionMessage()
        {
            return "I can't take any new possessions, but feel free to take back whatever you want.";
        }

        private string BuildInteractionMessage(GamePlayer player)
        {
            return $"{GetStartInteractionMessage(player)}{GetMiddleInteractionMessage()}{GetEndInteractionMessage()}";
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.Out.SendMessage(BuildInteractionMessage(player), eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            if (SetPlayerActiveInventoryObject(player))
                player.Out.SendInventoryItemsUpdate(player.ActiveInventoryObject.GetClientInventory(player), eInventoryWindowType.HouseVault);

            return true;
        }

        protected abstract bool TryGetHouseVault(GamePlayer player, out GameHouseVault vault);

        protected abstract bool SetPlayerActiveInventoryObject(GamePlayer player);

        protected static string ToOrdinalWord(int index)
        {
            return index switch
            {
                0 => "first",
                1 => "second",
                2 => "third",
                3 => "fourth",
                4 => "fifth",
                5 => "sixth",
                6 => "seventh",
                7 => "eighth",
                8 => "ninth",
                _ => index.ToString()
            };
        }

        protected static string ToCardinalWord(int index)
        {
            return index switch
            {
                0 => "one",
                1 => "two",
                2 => "three",
                3 => "four",
                4 => "five",
                5 => "six",
                6 => "seven",
                7 => "eight",
                8 => "nine",
                _ => index.ToString()
            };
        }
    }
}
