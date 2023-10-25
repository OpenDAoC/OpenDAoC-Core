using Core.GS.Enums;
using Core.GS.Expansions.Foundations;

namespace Core.GS.Commands;

[Command("&knock", //command to handle
	EPrivLevel.Player, //minimum privelege level
   "Knock on a house", //command description
	"/knock")] //command usage
public class KnockCommand : ACommandHandler, ICommandHandler
{
	public const string PLAYER_KNOCKED = "player_knocked_weak";
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player.CurrentHouse != null)
		{
			DisplayMessage(client, "You can't knock while your in a house!");
			return;
		}

		long KnockTick = client.Player.TempProperties.GetProperty<long>(PLAYER_KNOCKED);
		if (KnockTick > 0 && KnockTick - client.Player.CurrentRegion.Time <= 0)
		{
			client.Player.TempProperties.RemoveProperty(PLAYER_KNOCKED);
		}

		long changeTime = client.Player.CurrentRegion.Time - KnockTick;
		if (changeTime < 30000 && KnockTick > 0)
		{
			client.Player.Out.SendMessage("You must wait " + ((30000 - changeTime) / 1000).ToString() + " more seconds before knocking again!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		bool done = false;
		foreach (House house in HouseMgr.GetHousesCloseToSpot(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, 650))
		{
			client.Player.Emote(EEmote.Knock);
			foreach (GamePlayer player in house.GetAllPlayersInHouse())
			{
				string message = client.Player.Name + " is at your door";
				player.Out.SendMessage(message + "!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			done = true;
		}

		if (done)
		{
			client.Out.SendMessage("You knock on the door.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Player.TempProperties.SetProperty(PLAYER_KNOCKED, client.Player.CurrentRegion.Time);
		}
		else client.Out.SendMessage("You must go to the house you wish to knock on!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}
}