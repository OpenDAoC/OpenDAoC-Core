using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute("&knock",
        ePrivLevel.Player,
       "Knock on a house",
        "/knock")]
    public class KnockCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player.CurrentHouse != null || !client.Player.CurrentRegion.HousingEnabled)
                return;

            if (IsSpammingCommand(client.Player, "knock", 30000))
                return;

            bool done = false;

            foreach (House house in HouseMgr.GetHousesCloseToSpot(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, 650))
            {
                client.Player.Emote(eEmote.Knock);

                foreach (GamePlayer player in house.GetAllPlayersInHouse())
                    DisplayMessage(player, $"{client.Player.Name} is knocking on the door.");

                done = true;
            }

            if (done)
                DisplayMessage(client.Player, "You knock on the door.");
        }
    }
}
