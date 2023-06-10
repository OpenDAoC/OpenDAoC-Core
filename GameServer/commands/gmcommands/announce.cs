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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DOL.GS;
using DOL.GS.ServerProperties;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		// Enter '/announce' to list all commands of this type
		"&announce",
		// Message: '/announce' - Sends a message server-wide (to all players) using the specified delivery method.
		"GMCommands.Announce.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.GM,
		// Message: Sends a message server-wide (to all online players) using the specified delivery method.
		"GMCommands.Announce.Description",
		// Syntax: /announce center <message>
		"GMCommands.Announce.Syntax.Center",
		// Message: Sends a message to all players which displays in the center of their screen.
		"GMCommands.Announce.Usage.Center",
		// Syntax: /announce confirm <message>
		"GMCommands.Announce.Syntax.Confirm",
		// Message: Sends a dialog to all players which requires them to click 'OK' to confirm.
		"GMCommands.Announce.Usage.Confirm",
		// Syntax: /announce log <message>
		"GMCommands.Announce.Syntax.Log",
		// Message: Sends a message to all players which displays as Important in their System window.
		"GMCommands.Announce.Usage.Log",
		// Syntax: /announce send <message>
		"GMCommands.Announce.Syntax.Send",
		// Message: Sends a message to all players which displays as a Send message in their Chat window.
		"GMCommands.Announce.Usage.Send",
		// Syntax: /announce window <message>
		"GMCommands.Announce.Syntax.Window",
		// Message: Sends a text window to all players which displays the specified message.
		"GMCommands.Announce.Usage.Window"
	)]
	public class AnnounceCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 3)
			{
				DisplaySyntax(client);
				return;
			}

			string message = string.Join(" ", args, 2, args.Length - 2);
			if (message == "")
				return;

			switch (args.GetValue(1).ToString().ToLower())
			{
				#region Log
				case "log":
					{
						foreach (GameClient clients in WorldMgr.GetAllPlayingClients())
                            if(clients != null)
	                            // Message: [Announce]: {0}
	                            ChatUtil.SendTypeMessage(eMsg.Important, clients, "GMCommands.Announce.LogAnnounce", message);
						break;
					}
				#endregion Log
				#region Window
				case "window":
					{
						var messages = new List<string>();
						messages.Add(message);

						foreach (GameClient clients in WorldMgr.GetAllPlayingClients())
                            if(clients != null)
	                            // Message: Announce from {0}
	                            ChatUtil.SendWindowMessage(eWindow.Text, clients, "GMCommands.Announce.WindowAnnounce", client, messages);
						break;
					}
				#endregion Window
				#region Send
				case "send":
					{
						foreach (GameClient clients in WorldMgr.GetAllPlayingClients())
							if(clients != null)
	                            // Message: [Announce]: {0}
	                            ChatUtil.SendTypeMessage(eMsg.Send, clients, "GMCommands.Announce.SendAnnounce", message);
						break;
					}
				#endregion Send
				#region Center
				case "center":
					{
                        foreach (GameClient clients in WorldMgr.GetAllPlayingClients())
                            if (clients != null)
	                            ChatUtil.SendTypeMessage(eMsg.ScreenCenter, client, message);
						break;
					}
				#endregion Center
				#region Confirm
				case "confirm":
					{
						foreach (GameClient clients in WorldMgr.GetAllPlayingClients())
                            if (clients != null)
	                            // Message: Announce from {0}: {1}
	                            ChatUtil.SendTypeMessage(eMsg.DialogWarn, clients, "GMCommands.Announce.ConfirmAnnounce", client, message);
						break;
					}
				#endregion Confirm
				#region Default
				default:
					{
						DisplaySyntax(client);
						return;
					}
				#endregion Default
			}
		}
	}
}
