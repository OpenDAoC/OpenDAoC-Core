using System;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&speclock",
	EPrivLevel.GM,
	"Set your SpecMod combat modifier to a designated value <0.01 min>",
	"/speclock <value> - where value is a decimal input like 1.10 or 0.85", 
	"/speclock reset - clear value and use normal combat calculations")]

public class SpecLockCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}
			

		try
		{
			if (args[1].Equals("reset"))
			{
				client.Player.SpecLock = 0;
				return;
			}

			double input = Double.Parse(args[1]);
			if (input <= 0)
			{
				DisplaySyntax(client);
				return;
			}

			client.Player.SpecLock = input;
		}
		catch (Exception e)
		{
			Console.WriteLine(e + ": " + e.StackTrace);
			DisplaySyntax(client);
		}
	}
}