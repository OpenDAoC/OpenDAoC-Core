﻿using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&webdisplay",
	EPrivLevel.Player,
	"Set informations displayed on the herald",
	"/webdisplay <position|template|equipment|craft> [on|off]")]
public class WebDisplayCommand : ACommandHandler, ICommandHandler
{
	private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length == 1)
		{
			DisplayInformations(client);
			return;
		}
		
		if (args[1].ToLower() == "position")
			WdChange(GlobalConstants.EWebDisplay.position, client.Player, args.Length==3?args[2].ToLower():null);
		if (args[1].ToLower() == "equipment")
			WdChange(GlobalConstants.EWebDisplay.equipment, client.Player, args.Length==3?args[2].ToLower():null);
		if (args[1].ToLower() == "template")
			WdChange(GlobalConstants.EWebDisplay.template, client.Player, args.Length==3?args[2].ToLower():null);
		if (args[1].ToLower() == "craft")
			WdChange(GlobalConstants.EWebDisplay.craft, client.Player, args.Length==3?args[2].ToLower():null);
		
		DisplayInformations(client);
	}

	// Set the eWebDisplay status
	private void WdChange(GlobalConstants.EWebDisplay category, GamePlayer player, string state)
	{
		if (string.IsNullOrEmpty(state))
			player.NotDisplayedInHerald ^= (byte)category;
		else
		{
			if (state == "off")
				player.NotDisplayedInHerald |= (byte)category;
			
			if (state == "on")
				player.NotDisplayedInHerald &= (byte)~category;
		}
		
		log.Debug("Player " + player.Name + ": WD = " + player.NotDisplayedInHerald);
	}

	// Display the informations
	private void DisplayInformations(GameClient client)
	{
		byte webDisplay = client.Player.NotDisplayedInHerald;
		byte webDisplayFlag;

		string state = "/webdisplay <position|template|equipment|craft> [on|off]\n";
		
		webDisplayFlag = (byte)GlobalConstants.EWebDisplay.equipment;
		if ((webDisplay & webDisplayFlag) == webDisplayFlag)
			state += "Your equipment is not displayed.\n";
		else
			state += "Your equipment is displayed.\n";
		
		webDisplayFlag = (byte)GlobalConstants.EWebDisplay.position;
		if ((webDisplay & webDisplayFlag) == webDisplayFlag)
			state += "Your position is not displayed.\n";
		else
			state += "Your position is displayed.\n";
		
		webDisplayFlag = (byte)GlobalConstants.EWebDisplay.template;
		if ((webDisplay & webDisplayFlag) == webDisplayFlag)
			state += "Your template is not displayed.\n";
		else
			state += "Your template is displayed.\n";

		webDisplayFlag = (byte)GlobalConstants.EWebDisplay.craft;
		if ((webDisplay & webDisplayFlag) == webDisplayFlag)
			state += "Your crafting skill is not displayed.\n";
		else
			state += "Your crafting skill is displayed.\n";		
		
		DisplayMessage(client, state);
	}
}