
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Cmd(
		"&api",
		ePrivLevel.Player,
		"Toggles API options",
		"/api specs - toggle showing the specs of the player")]
	public class APICommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
			
			if (IsSpammingCommand(client.Player, "api"))
				return;

			if (args[1].ToLower() == "specs")
			{
				client.Player.HideSpecializationAPI = !client.Player.HideSpecializationAPI;
				client.Out.SendMessage("API specialization details: " + (client.Player.HideSpecializationAPI ? "hidden" : "shown"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				GameServer.Database.SaveObject(client.Player.DBCharacter); // using this instead of SaveIntoDatabase() because we don't want to display it to the player to avoid save abuse
			}
		}
	}
}