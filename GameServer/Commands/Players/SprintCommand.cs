using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Command(
		"&sprint",
		EPrivLevel.Player,
		"Toggles sprint mode",
		"/sprint")]
	public class SprintCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player.HasAbility(Abilities.Sprint))
			{
				client.Player.Sprint(!client.Player.IsSprinting);
			}
			else
			{
				client.Out.SendMessage("You do not have a sprint ability.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
	}
}