using DOL.GS.Scripts;


namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&zonebonus",
		new string[] {"&zb"},
		ePrivLevel.GM,
		"Picks a new bonus zone",
		"/zonebonus or /zb")]
	public class ZoneBonusCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			ZoneBonusRotator.GetZones();
			ZoneBonusRotator.UpdatePvEZones();
			ZoneBonusRotator.UpdateRvRZones();
		}
	}
}