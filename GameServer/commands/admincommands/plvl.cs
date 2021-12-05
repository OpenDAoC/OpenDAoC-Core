/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

/* <--- SendMessage Standardization --->
*  All messages now use translation IDs to both
*  centralize their location and standardize the method
*  of message calls used throughout this project. All messages affected
*  are in English. Other languages are not yet supported.
* 
*  To  find a message at its source location, either use
*  the message body contained in the comment above the return
*  (e.g., // Message: "This is a message.") or the
*  translation ID (e.g., "AdminCommands.Account.Description").
* 
*  To perform message changes, take note of your server settings.
*  If the `serverproperty` table setting `use_dblanguage`
*  is set to `True`, you must make your changes from the
*  `languagesystem` DB table.
* 
*  If the `serverproperty` table setting
*  `update_existing_db_system_sentences_from_files` is set to `True`,
*  perform changes to messages from this file at "GameServer >
*  language > EN > OtherSentences.txt" and "Commands > AdminCommands.txt".
*
*  OPTIONAL: After changing a message, paste the new content
*  into the comment above the affected message return(s). This is
*  done for ease of reference. */

using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	// See the comments above 'using' about SendMessage translation IDs
	[CmdAttribute(
		// Enter '/plvl' to list all commands of this type
		"&plvl",
		// Message: <----- '/plvl' Commands (plvl 3) ----->
		"AdminCommands.Header.Syntax.Plvl",
		ePrivLevel.Admin,
		// Message: "Alters an account's privilege level (plvl) and grants/revokes access to command types depending on the user's plvl. With these commands, accounts may be granted Admin, GM, and Player command access from in-game. These commands are intended for testing purposes and should not be used on non-Atlas staff accounts."
		"AdminCommands.Plvl.Description",
		// Syntax: /plvl command
		"AdminCommands.Plvl.Syntax.Comm",
		// Message: "Provides additional information regarding the '/plvl' command type."
		"AdminCommands.Plvl.Usage.Comm",
		// Syntax: /plvl <newPlvl> <playerName>
		"AdminCommands.Plvl.Syntax.Plvl",
		// Message: "Sets the privilege level for a targeted player's account. The player will then have access to all commands associated with that privilege level. Use '/plvl single' or '/plvl singleaccount' to grant access to specific commands as a Player."
		"AdminCommands.Plvl.Usage.Plvl",
		// Syntax: /plvl remove <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.Remove",
		// Message: "Removes a specific permission previously granted to a player using the '/plvl single' command."
		"AdminCommands.Plvl.Usage.Remove",
		// Syntax: /plvl removeaccount <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.AcctRemove",
		// Message: "Removes a specific permission previously granted to a player's account using the '/plvl singleaccount' command."
		"AdminCommands.Plvl.Usage.AcctRemove",
		// Syntax: /plvl single <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.Single",
		// Message: "Grants a character the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.)."
		"AdminCommands.Plvl.Usage.Single",
		// Syntax: /plvl singleaccount <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.AcctSingle",
		// Message: "Grants all characters on a player's account the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.)."
		"AdminCommands.Plvl.Usage.AcctSingle")]
	public class PlvlCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists '/plvl' commands' syntax (see '&plvl' section above)
				DisplaySyntax(client);
				return;
			}

			GamePlayer target = client.Player;

			switch (args[1])
			{
				#region Single

				// Grants a single command type to the specified player that may be used no matter their privilege level
				// Syntax: /plvl single <commandType> <playerName>
				// Args:   /plvl args[1] args[2]      args[3]
				// See the comments above 'using' about SendMessage translation IDs
				case "single":
				{
					switch (args.Length)
					{
						// Display syntax for '/plvl single'
						case 2:
						{
							// Message: "<----- '/plvl' Commands (plvl 3) ----->"
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Plvl", null);
							// Message: "Use the following syntax for this command:"
							ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxDesc", null);
							// Syntax: /plvl single <commandType> <playerName>
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Plvl.Syntax.Single", null);
							// Message: "Grants a character the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.)."
							ChatUtil.SendCommMessage(client, "AdminCommands.Plvl.Usage.Single", null);
							return;
						}
						// Player is not specified
						case 3:
						{
							// Message: "You must specify a player name for '/plvl' commands!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerName", null);
							return;
						}
						// Player name specified
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							if (targetClient == null)
							{
								// Message: "No player is online with the name '{0}'. Please make sure that you entered the whole player's name and they are online."
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerExists", args[3]);
								return;
							}

							// Target is the player logged in
							target = targetClient.Player;

							// Adds the command type to the 'singlepermission' table
							SinglePermission.setPermission(targetClient.Player, args[2]);
							// Message: "You've granted {0} access to the '/{1}' command!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Msg.AddSinglePerm", target.Name, args[2]);

							if (target != client.Player)
							{
								// Message: "{0} has given you access to the '/{1}' command! Type '/{1}' to view the command list."
								ChatUtil.SendErrorMessage(target.Client, "AdminCommands.Plvl.Msg.GaveSinglePerm", client.Player.Name, args[2]);
							}
							return;
						}
					}

					return;
				}

				#endregion Single

				#region Single Account
				// Grants a command type to all existing characters on the specified player's account
				// Syntax: /plvl singleaccount <commandType> <playerName>
				// Args:   /plvl args[1]       args[2]       args[3]
				// See the comments above 'using' about SendMessage translation IDs
				case "singleaccount":
				{
					switch (args.Length)
					{
						// If only '/plvl singleaccount' is entered
						case 2:
						{
							// Message: "<----- '/plvl' Commands (plvl 3) ----->"
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Plvl", null);
							// Message: "Use the following syntax for this command:"
							ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxDesc", null);
							// Syntax: /plvl singleaccount <commandType> <playerName>
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Plvl.Syntax.AcctSingle", null);
							// Message: "Grants all characters on a player's account the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.)."
							ChatUtil.SendCommMessage(client, "AdminCommands.Plvl.Usage.AcctSingle", null);
							return;
						}
						// If only '/plvl singleaccount <commandName>' is entered
						case 3:
						{
							// Message: "You must specify a player name for '/plvl' commands!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerName", null);
							return;
						}
						// If full command is entered
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							if (targetClient == null)
							{
								// Message: "No player is online with the name '{0}'. Please make sure that you entered the whole player's name and they are online."
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerExists", args[3]);
								return;
							}

							target = targetClient.Player;

							SinglePermission.setPermissionAccount(target, args[2]);
							// Message: "You have granted {0}'s account access to the '/{1}' command!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Msg.AddAcct", target.Name, args[2]);

							// Sends message to target
							if (target != client.Player)
							{
								// Message: "{0} has given your account access to the '/{1}' command! Type '/{1}' to view the command list."
								ChatUtil.SendErrorMessage(target.Client, "AdminCommands.Plvl.Msg.GaveAcct", client.Player.Name, args[2]);
							}
							return;
						}
					}
					return;
				}
				#endregion Single Account

				#region Remove
				// Revokes a command type from the specified character
				// Syntax: /plvl remove <commandType> <playerName>
				// Args:   /plvl args[1] args[2]      args[3]
				// See the comments above 'using' about SendMessage translation IDs
				case "remove":
				{
					switch (args.Length)
					{
						// If only '/plvl remove' is entered
						case 2:
						{
							// Message: "<----- '/plvl' Commands (plvl 3) ----->"
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Plvl", null);
							// Message: "Use the following syntax for this command:"
							ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxDesc", null);
							// Syntax: /plvl remove <commandType> <playerName>
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Plvl.Syntax.Remove", null);
							// Message: "Removes a specific command type previously granted to a player using '/plvl single'."
							ChatUtil.SendCommMessage(client, "AdminCommands.Plvl.Usage.Remove", null);
							return;
						}
						// If only '/plvl remove <commandType>' is entered
						case 3:
						{
							// Message: "You must specify a player name for '/plvl' commands!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerName", null);
							return;
						}
						// If full command is entered
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							// If the account doesn't exist
							if (targetClient == null)
							{
								// Message: "No player is online with the name '{0}'. Please make sure that you entered the whole player's name and they are online."
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerExists", args[3]);
								return;
							}

							// Target is the player logged in
							target = targetClient.Player;

							// If player has permission, remove it
							if (SinglePermission.HasPermission(target, args[2]))
							{
								// Removes the command type to the 'singlepermission' table
								SinglePermission.removePermission(target, args[2]);
							}
							// If player doesn't have permission
							else
							{
								// Message: "No permission has been granted to {0} for the '/{1}' command."
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPermFound",
									target.Name, args[2]);
								return;
							}

							// Message: "You have revoked {0}'s access to the '/{1}' command!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Msg.RevokeSinglePerm", target.Name, args[2]);

							// If the target isn't player executing command
							if (target != client.Player)
							{
								// Message: "{0} has removed your access to the '/{1}' command! You may no longer use this command type while assigned the Player privilege level."
								ChatUtil.SendErrorMessage(target.Client, "AdminCommands.Plvl.Msg.DelSinglePerm",
									client.Player.Name, args[2]);
								return;
							}
						}
						return;
					}
					break;
				} 
			
				#endregion Remove

				#region Remove Account
				// Revokes a command type from the specified character's account
				// Syntax: /plvl removeaccount <commandType> <playerName>
				// Args:   /plvl args[1]       args[2]       args[3]
				// See the comments above 'using' about SendMessage translation IDs
                case "removeaccount":
                {
	                switch (args.Length)
					{
						// If '/plvl removeaccount' is entered
						case 2:
						{
							// Message: "<----- '/plvl' Commands (plvl 3) ----->"
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Plvl", null);
							// Message: "Use the following syntax for this command:"
							ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxDesc", null);
							// Syntax: /plvl removeaccount <commandType> <playerName>
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Plvl.Syntax.AcctRemove", null);
							// Message: "Removes a specific permission previously granted to a player's account using '/plvl singleaccount'."
							ChatUtil.SendCommMessage(client, "AdminCommands.Plvl.Usage.AcctRemove", null);
							return;
						}
						// If '/plvl removeaccount <commandType>' is entered
						case 3:
						{
							// Message: "You must specify a player name for '/plvl' commands!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerName", null);
							return;
						}
						// If full command is entered
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							// If the account doesn't exist
							if (targetClient == null)
							{
								// Message: "No player is online with the name '{0}'. Please make sure that you entered the whole player's name and they are online."
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerExists", args[3]);
								return;
							}

							// Target is the player logged in
							target = targetClient.Player;

							// If player's account has the permission, remove it from the 'singlepermission' table
							if (SinglePermission.HasPermission(target, args[2]))
							{
								SinglePermission.removePermissionAccount(target, args[2]);
							}
							// If player's account doesn't have permission
							else
							{
								// Message: "No permission was found for {0}'s account and the '/{1}" command!"
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoAcctPermFound", target.Name, args[2]);
								return;
							}

							// Message: "You have revoked {0}'s account access to the '/{1}' command!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Msg.RevokeAcctPerm", target.Name, args[2]);

							// If the target isn't player executing command
							if (target != client.Player)
							{
								// Message: "{0} has removed your account's access to the '/{1}' command! Your characters may no longer use this command type while assigned the Player privilege level."
								ChatUtil.SendErrorMessage(target.Client, "AdminCommands.Plvl.Msg.DelAcctPerm", client.Player.Name, args[2]);
								return;
							}
						}
						return;
					}
	                break;
                }
				#endregion Remove Account

				#region Plvl
				// Sets the privilege level for a player's account
				// Syntax: /plvl <newPlvl> <playerName>
				// Args:   /plvl args[1]   args[2]
				// See the comments above 'using' about SendMessage translation IDs
				default:
					{
						uint plvl = 1;

						// If an unsupported value is entered in place of '<newPlvl>'
						if (!uint.TryParse(args[1], out plvl))
						{
							// Message: "<----- '/plvl' Commands (plvl 3) ----->"
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Plvl", null);
							// Message: "If you are unable to access the Atlas Web Admin tool (https://admin.atlasfreeshard.com) to perform this action, use the following syntax:"
							ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxCRUD", null);
							// Syntax: /plvl <newPlvl> <playerName>
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Plvl.Syntax.Plvl", null);
							// Message: "Sets the privilege level for a targeted player's account. They will then have access to all commands associated with that role. Use '/plvl single' or '/plvl singleaccount' to retain access to specific command types as a Player."
							ChatUtil.SendCommMessage(client, "AdminCommands.Plvl.Usage.Plvl", null);
							return;
						}
						// If a player name is specified
						if (args.Length > 2)
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[2], true, true);

							if (targetClient == null) 
							{
								// Message: "No player is online with the name '{0}'. Please make sure that you entered the whole player's name and they are online."
								ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Err.NoPlayerExists", args[2]);
								return;
							}
							
							target = targetClient.Player;
						}
						// If plvl specified is Player or GM and no target is specified
						if (args[1] == "1" || args[1] == "2" && client.Player == target && target == null)
						{
							// If player's account doesn't have 'plvl' permission
							if (SinglePermission.HasPermission(client.Player, "plvl") == false)
							{
								// Message: "You do not have the '/plvl' permission assigned! You will be unable to access the '/plvl' command again as a GM or Player."
								client.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, LanguageMgr.GetTranslation(client, "AdminCommands.Plvl.Err.NoPlvlPerm", null));
								return;
							}
						}
						
						if (target != null)
						{
							target.Client.Account.PrivLevel = plvl;

							// Sets the privilege level to the character's account, saves to DB, and refreshes world for character to reflect the change
							GameServer.Database.SaveObject(target.Client.Account);
							client.Player.RefreshWorld();
							// Refresh equipment for player so they don't appear naked after changing plvl
							client.Player.UpdateEquipmentAppearance();

							// Message: "You have changed {0}'s account privilege level to {1}!"
							ChatUtil.SendErrorMessage(client, "AdminCommands.Plvl.Msg.PlvlSet", target.Name,
								plvl.ToString());

							if (target != client.Player)
								// Message: "{0} has changed your account's privilege level to {1}!"
								ChatUtil.SendErrorMessage(target.Client, "AdminCommands.Plvl.Msg.YourPlvlSet",
									client.Player.Name, plvl.ToString());
							return;
						}
						break;
					}
				#endregion Plvl

				#region Command
				// Provides additional information regarding the '/plvl' command type
				// Syntax: /plvl command
				// Args:   /plvl args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "command":
					{
						// Displays dialog with information
						var info = new List<string>();
						info.Add(" ");
						// Message: "----- Privilege Levels -----"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.PrivLevels"));
						info.Add(" ");
						// Message: "The '/plvl' command type allows you to control an account's privilege level and the command types its characters may access when they are a Player. The values used for each plvl are:"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.Intro"));
						info.Add(" ");
						// Message: "1 = Player"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.1"));
						// Message: "You can be attacked by mobs or other players and take falling damage. Players only have access to basic slash commands (such as '/bg', '/gc', '/cg', etc.)."
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.Usage1"));
						// Message: "2 = Gamemaster (GM)"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.2"));
						// Message: "You cannot be attacked by mobs or other players or take falling damage. GMs have access to MOST special slash commands (except those requiring ePrivLevel.Admin--such as '/plvl', '/account', '/shutdown', etc.)."
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.Usage2"));
						// Message: "3 = Admin"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.3"));
						// Message: "You cannot be attacked by mobs or other players or take falling damage. Admins have access to ALL special slash commands (including GM)."
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.Usage3"));
						info.Add(" ");
						// Message: "These integers are used when changing your priv level ('/plvl <newPlvl> <playerName>') to test functionality or combat in-game."
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.UseSingleAcct"));
						info.Add(" ");
						// Message: "--- NOTE: ---"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AllCommands.Header.Note.Divider"));
						// Message: "To retain access to specific command types as a Player, enter '/plvl single' or '/plvl singleaccount' for details."
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Note.PlvlSingle"));
						// Message: "--------------"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AllCommands.Header.Note.Dashes"));
						info.Add(" ");
						// Message: "----- Additional Info -----"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.MoreInfo"));
						info.Add(" ");
						// Message: "For more information regarding the '/plvl' command type, see page 22 (post #430) of the GM Commands Library on the Atlas Developers forum."
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Plvl.Comm.Desc"));
						info.Add(" ");
						// Message: "https://www.atlasfreeshard.com/threads/gm-commands-library.408/post-4379"
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Hyperlinks.CommLibrary.Plvl"));
						info.Add(" ");
			
						client.Out.SendCustomTextWindow("Using the '/plvl' Command Type", info);
						
						return;
					}
				#endregion Command
				
			}
		}
	}
}