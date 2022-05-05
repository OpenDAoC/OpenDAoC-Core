using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&craft",
		ePrivLevel.Player,
		"Crafting macros and utilities",
		"'/craft set <#>' to set how many items you want to craft",
		"'/craft clear' to reset to crafting once",
		"'/craft show' to show the current craft settings")]
	public class CraftMacroCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public const string CraftQueueLength = "CraftQueueLength";
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length >= 2)
			{
				if (args[1] == "set")
				{
					if (args.Length >= 3)
					{
						int.TryParse(args[2], out int count);
						if (count == 0)
						{
							client.Out.SendMessage("Use: /craft set <#>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						client.Player.TempProperties.setProperty(CraftQueueLength, count);
						client.Out.SendMessage($"Crafting queue set to {count} items", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else
					{
						client.Out.SendMessage("Use: /craft set <#>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
				}

				if (args[1].Contains("clear"))
				{
					if (client.Player.TempProperties.getProperty<int>(CraftQueueLength) != 0)
						client.Out.SendMessage("Crafting queue reset to 1", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
						client.Out.SendMessage("The crafting queue is already set to 1", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				
				if (args[1].Contains("show"))
				{
					if (client.Player.TempProperties.getProperty<int>(CraftQueueLength) != 0)
						client.Out.SendMessage($"Crafting queue set to {client.Player.TempProperties.getProperty<int>(CraftQueueLength)}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
						client.Out.SendMessage("Crafting queue set to 1", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
			else
			{
				client.Out.SendMessage("Use: `/craft set <#>', `/craft clear', `/craft show'", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}