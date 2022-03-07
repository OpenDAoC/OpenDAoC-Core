using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&setwho",
		ePrivLevel.Player,
		"Set your class or trade for /who output",
		"/setwho class | trade")]
	public class SetWhoCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "setwho"))
				return;

			if (args.Length < 2)
			{
				DisplayMessage(client, "You need to specify if you want to change to class or trade");
				return;
			}
			
			var played = client.Player.PlayedTimeSinceLevel / 60 / 60; // Sets time played since last level
			var totalPlayed = client.Player.PlayedTime / 60 / 60; // Time played total for character
			
			if (client.Player.Level == 50 && played < 15 && args[1].ToLower() == "class" && !client.Player.ClassNameFlag && client.Player.Advisor || client.Player.Level != 50 && totalPlayed < 15 && client.Player.Advisor && args[1].ToLower() == "class" && !client.Player.ClassNameFlag)
			{
				// Message: "You cannot turn off your craft title while your Advisor flag is active, as you do not meet the other level and/or time played requirements."
				ChatUtil.SendSystemMessage(client, "PLCommands.SetWho.Err.CraftAdvisor", null);
				return;
			}

			if (args[1].ToLower() == "class")
				client.Player.ClassNameFlag = true;
			else if (args[1].ToLower() == "trade")
			{
				if (client.Player.CraftingPrimarySkill == eCraftingSkill.NoCrafting)
				{
					DisplayMessage(client, "You need a profession to enable it in for who messages");
					return;
				}

				client.Player.ClassNameFlag= false;
			}
			else
			{
				DisplayMessage(client, "You need to specify if you want to change to class or trade");
				return;
			}

			if (client.Player.ClassNameFlag)
				DisplayMessage(client, "/who will no longer show your crafting title");
			else
				DisplayMessage(client, "/who will now show your crafting title");
		}
	}
}