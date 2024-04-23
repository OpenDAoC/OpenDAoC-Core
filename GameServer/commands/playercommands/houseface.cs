using DOL.GS.Housing;

namespace DOL.GS.Commands
{
    [CmdAttribute(
      "&houseface",
      ePrivLevel.Player,
      "Points to the specified guildhouse of the guild noted, or the lot number noted in the command. /houseface alone will point to one's personal home.")]
    public class HouseFaceCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            int houseNumber;

            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out houseNumber))
                {
                    DisplaySyntax(client);
                    return;
                }
            }
            else
                houseNumber = HouseMgr.GetHouseNumberByPlayer(client.Player);

            House house = HouseMgr.GetHouse(houseNumber);

            if (house == null)
            {
                DisplayMessage(client, "No house found.");
                return;
            }

            ushort direction = client.Player.GetHeading(house);
            client.Player.Heading = direction;
            client.Out.SendPlayerJump(true);
            DisplayMessage(client, $"You face house {houseNumber}.");
        }
    }
}
