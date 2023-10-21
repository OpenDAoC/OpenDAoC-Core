using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&say",
	new string[] {"&s"},
	EPrivLevel.Player,
	"Say something to other players around you",
	"/say <message>")]
public class SayCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		const string SAY_TICK = "Say_Tick";

		if (args.Length < 2)
		{
			client.Out.SendMessage("You must say something...", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
		string message = string.Join(" ", args, 1, args.Length - 1);

		long SayTick = client.Player.TempProperties.GetProperty<long>(SAY_TICK);
		if (SayTick > 0 && SayTick - client.Player.CurrentRegion.Time <= 0)
		{
			client.Player.TempProperties.RemoveProperty(SAY_TICK);
		}

		long changeTime = client.Player.CurrentRegion.Time - SayTick;
		if (changeTime < 500 && SayTick > 0)
		{
			client.Player.Out.SendMessage("Slow down! Think before you say each word!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
        if (client.Player.IsMuted)
        {
            client.Player.Out.SendMessage("You have been muted. You cannot talk.", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
            return;
        }

		client.Player.Say(message);
		client.Player.TempProperties.SetProperty(SAY_TICK, client.Player.CurrentRegion.Time);
	}
}