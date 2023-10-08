using System;
using System.Collections.Generic;
using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"&harm",
	EPrivLevel.GM,
	"GMCommands.Harm.Description",
	"GMCommands.Harm.Usage")]
public class HarmCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length == 1)
		{
			DisplaySyntax(client);
			return;
		}

		int amount;

		try
		{
			amount = Convert.ToInt32(args[1]);

			if (client.Player.TargetObject is GameLiving living)
				living.TakeDamage(client.Player, EDamageType.GM, amount, 0);
			else
				DisplayMessage(client,
					LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Harm.InvalidTarget"));
		}
		catch (Exception ex)
		{
			List<string> list = new();
			list.Add(ex.ToString());
			client.Out.SendCustomTextWindow("Exception", list);
		}
	}
}