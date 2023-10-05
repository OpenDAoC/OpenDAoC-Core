using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&yell",
	new string[] { "&y" },
	ePrivLevel.Player,
	"Yell something to other players around you",
	"/yell <message>")]
public class YellCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		const string YELL_TICK = "YELL_Tick";
		long YELLTick = client.Player.TempProperties.GetProperty<long>(YELL_TICK);
		if (YELLTick > 0 && YELLTick - client.Player.CurrentRegion.Time <= 0)
		{
			client.Player.TempProperties.RemoveProperty(YELL_TICK);
		}

		long changeTime = client.Player.CurrentRegion.Time - YELLTick;
		if (changeTime < 750 && YELLTick > 0)
		{
			DisplayMessage(client, "Slow down! Think before you say each word!");
			return;
		}
        if (client.Player.IsMuted)
        {
            client.Player.Out.SendMessage("You have been muted. You cannot yell.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            return;
        }

		if (args.Length < 2)
		{
			foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.YELL_DISTANCE))
			{
				if (player != client.Player)
				{
					ushort headingtemp = player.GetHeading(client.Player);
					ushort headingtotarget = (ushort)(headingtemp - player.Heading);
					string direction = "";
					if (headingtotarget < 0)
						headingtotarget += 4096;
					if (headingtotarget >= 3840 || headingtotarget <= 256)
						direction = "South";
					else if (headingtotarget > 256 && headingtotarget < 768)
						direction = "South West";
					else if (headingtotarget >= 768 && headingtotarget <= 1280)
						direction = "West";
					else if (headingtotarget > 1280 && headingtotarget < 1792)
						direction = "North West";
					else if (headingtotarget >= 1792 && headingtotarget <= 2304)
						direction = "North";
					else if (headingtotarget > 2304 && headingtotarget < 2816)
						direction = "North East";
					else if (headingtotarget >= 2816 && headingtotarget <= 3328)
						direction = "East";
					else if (headingtotarget > 3328 && headingtotarget < 3840)
						direction = "South East";
					player.Out.SendMessage(client.Player.Name + " yells for help from the " + direction + "!", eChatType.CT_Help, eChatLoc.CL_SystemWindow);
				}
				else
					client.Out.SendMessage("You yell for help!", eChatType.CT_Help, eChatLoc.CL_SystemWindow);
			}
			client.Player.TempProperties.SetProperty(YELL_TICK, client.Player.CurrentRegion.Time);
			return;
		}

		string message = string.Join(" ", args, 1, args.Length - 1);
		client.Player.Yell(message);
		client.Player.TempProperties.SetProperty(YELL_TICK, client.Player.CurrentRegion.Time);
		return;
	}
}