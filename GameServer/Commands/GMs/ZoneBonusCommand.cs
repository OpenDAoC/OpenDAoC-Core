using DOL.GS.Scripts;

namespace DOL.GS.Commands;

[Command(
	"&zonebonus",
	new string[] {"&zb"},
	EPrivLevel.GM,
	"Picks a new bonus zone",
	"/zonebonus or /zb")]
public class ZoneBonusCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		ZoneBonusRotator.GetZones();
		ZoneBonusRotator.UpdatePvEZones();
		ZoneBonusRotator.UpdateRvRZones();
	}
}