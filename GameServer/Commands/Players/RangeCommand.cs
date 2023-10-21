using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"&range",
	EPrivLevel.Player,
	"Gives a range to a target",
	"/range")]
public class RangeCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "range"))
			return;

		GameLiving living = client.Player.TargetObject as GameLiving;
		if (client.Player.TargetObject == null)
			DisplayMessage(client, (LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Range.NeedTarget")));
		else if (living == null || (living != null && client.Account.PrivLevel > 1))
		{
			int range = client.Player.GetDistanceTo( client.Player.TargetObject );
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Range.Result", range, (client.Player.TargetInView ? "" : LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Range.NotVisible"))));
		}
		else
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Range.InvalidObject"));
	}
}