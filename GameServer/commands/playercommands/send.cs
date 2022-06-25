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

using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&send",
		new [] { "&tell", "&t" },
		ePrivLevel.Player,
		// Displays next to the command when '/cmd' is entered
		"Sends a private message to the target player.",
		"PLCommands.SendMessage.Syntax.Send")]
	public class SendCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 3)
			{
				// Message: '/send <targetName> <message>' - Sends a private message to the target player.
				ChatUtil.SendSystemMessage(client, "PLCommands.SendMessage.Syntax.Send", null);
				return;
			}

			if (IsSpammingCommand(client.Player, "send", 500))
			{
				// Message: "Slow down, you're typing too fast--make the moment last."
				ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.SlowDown", null);
				return;
			}

			string targetName = args[1];
			var name = !string.IsNullOrWhiteSpace(targetName) && char.IsLower(targetName, 0) ? targetName.Replace(targetName[0],char.ToUpper(targetName[0])) : targetName; // If first character in args[1] is lowercase, replace with uppercase character
			string message = string.Join(" ", args, 2, args.Length - 2);

			int result = 0;
			GameClient targetClient;
			
			if (client.Account.PrivLevel > 1)
			{
				targetClient = WorldMgr.GuessClientByPlayerNameAndRealm(targetName, 0, false, out result);
			}
			else
			{
				targetClient = WorldMgr.GuessClientByPlayerNameAndRealm(targetName, client.Player.Realm, false, out result);
			}

			if (targetClient != null && !GameServer.ServerRules.IsAllowedToUnderstand(client.Player, targetClient.Player))
			{
				targetClient = null;
			}

			if (targetClient == null)
			{
				// Message: "{0} is not in the game, or is a member of another realm."
				ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
				return;
			}

            // prevent to send an anon GM a message to find him - but send the message to the GM - thx to Sumy
            if (targetClient.Player != null && targetClient.Player.IsAnonymous && targetClient.Account.PrivLevel > (uint)ePrivLevel.Player && targetClient != client)
            {
				if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
				{
					// Message: "{0} is not in the game, or is a member of another realm."
					ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
					// Message: {0} tried to send you a message: "{1}"
					ChatUtil.SendSendMessage(targetClient.Player, "Social.ReceiveMessage.Staff.TriedToSend", client.Player.Name, message);
				}
				if (client.Account.PrivLevel > (uint)ePrivLevel.Player)
				{
					// Let staff ignore anon state for other staff members
					// Message: You send, "{0}" to {1} [ANON].
					ChatUtil.SendSendMessage(client, "Social.SendMessage.Staff.YouSendAnon", message, targetClient.Player.Name);
					// Message: {0} [TEAM] sends, "{1}"
					ChatUtil.SendGMMessage(targetClient.Player, "Social.ReceiveMessage.Staff.SendsToYou", client.Player.Name, message);
				}
                return;
            }

			switch (result)
			{
				case 2: // Name not unique based on partial entry
					// Message: "{0} is not a unique character name."
					ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.NameNotUnique", name);
					return;
				case 3: // Exact name match
				case 4: // Guessed name based on partial entry
					if (targetClient == client)
					{
						// Message: "You can't message yourself!"
						ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.CantMsgYourself", null);
					}
					else
					{
						// Send the message
						client.Player.SendPrivateMessage(targetClient.Player, message);
					}
					return;
			}
		}
	}
}