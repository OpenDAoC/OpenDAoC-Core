using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"&autoloot",
	ePrivLevel.Player,
	"Automatically pick up any loot that drops in your area.",
	"/autoloot <on/off>")]
public class AutoLootCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		if (args[1].ToLower().Equals("on"))
		{
			client.Player.Autoloot = true;
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autoloot.On"));
        }
		else if (args[1].ToLower().Equals("off"))
		{
			client.Player.Autoloot = false;
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autoloot.Off"));
        }
	}
}