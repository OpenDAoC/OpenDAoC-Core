using DOL.GS.Housing;

namespace DOL.GS.Commands;

[Command(
  "&housepoints",
  ePrivLevel.Player,
   "Toggles display of housepoints",
	 "Useage: /housepoints toggle")]
public class HousePointsCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
        House house = client.Player.CurrentHouse;
		if (!client.Player.InHouse || house == null)
		{
            DisplayMessage(client, "You need to be in a House to use this command!");
			return;
		}

        if (!house.HasOwnerPermissions(client.Player))
        {
            DisplayMessage(client, "You do not have permissions to do that!");
            return;
        }

		client.Player.Out.SendToggleHousePoints(house);
	}
}