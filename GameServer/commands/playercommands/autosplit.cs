using DOL.Language;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute("&autosplit",
		 ePrivLevel.Player,
		 "Choose how the loot and money are split between members of group",
		 "/autosplit on/off (Leader only: Toggles both coins and loot for entire group)",
		 "/autosplit coins (Leader only: When turned off, will send coins to the person who picked it up, instead of splitting it evenly across other members)",
		 "/autosplit loot (Leader only: When turned off, will send loot to the person who picked it up, instead of splitting it evenly across other members)",
		 "/autosplit self (Any group member: Choose not to receive autosplit loot items)")]
	public class AutosplitCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			// If they are not in a group, then this command should not work at all
			if (client.Player.Group == null)
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.InGroup"));
				return;
			}

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			string command = args[1].ToLower();

			// /autosplit for leaders -- Make sue it is the group leader using this command, if it is, execute it.
			if (command == "on" || command == "off" || command == "coins" || command == "loot")
			{
				if (client.Player != client.Player.Group.Leader)
				{
					DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.Leader"));
					return;
				}

				switch (command)
				{
					case "on":
						{
							client.Player.Group.AutosplitLoot = true;
							client.Player.Group.AutosplitCoins = true;
							client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.On"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}

					case "off":
						{
							client.Player.Group.AutosplitLoot = false;
							client.Player.Group.AutosplitCoins = false;
							client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.Off"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
					case "coins":
						{
							client.Player.Group.AutosplitCoins = !client.Player.Group.AutosplitCoins;
							client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.Coins") + (client.Player.Group.AutosplitCoins ? " on" : " off") + " the autosplit coin", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
					case "loot":
						{
							client.Player.Group.AutosplitLoot = !client.Player.Group.AutosplitLoot;
							client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.Loot") + (client.Player.Group.AutosplitCoins ? " on" : "off") + " the autosplit coin", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
				}
				return;
			}

			// /autosplit for Members including leader -- 
			if (command == "self")
			{
				client.Player.AutoSplitLoot = !client.Player.AutoSplitLoot;
				client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Autosplit.Self", client.Player.Name) + (client.Player.AutoSplitLoot ? " on" : " off") + " their autosplit loot", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			//if nothing matched, then they tried to invent thier own commands -- show syntax
			DisplaySyntax(client);
		}
	}
}