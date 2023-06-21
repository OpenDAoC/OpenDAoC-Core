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

using System;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		// Enter '/ban' to list all associated subcommands
		"&ban",
		// Message: '/ban' - Bans the player's account or current IP address.
		"AdminCommands.Ban.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.GM,
		// Message: Bans the account or IP address by player name or client ID.
		"GMCommands.Ban.Description",
		// Message: /ban account <playerName|client#> <reason>
		"GMCommands.Ban.Syntax.Account",
		// Message: Bans the player's account indefinitely.
		"GMCommands.Ban.Usage.Account",
		// Message: /ban both <playerName|client#> <reason>
		"GMCommands.Ban.Syntax.Both",
		// Message: Bans the player's account and current IP address indefinitely.
		"GMCommands.Ban.Usage.Both",
		// Message: /ban ip <playerName|client#> <reason>
		"GMCommands.Ban.Syntax.IPAddress",
		// Message: Bans the IP address the player is currently accessing.
		"GMCommands.Ban.Usage.IPAddress"
	)]
	public class BanCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void OnCommand(GameClient client, string[] args)
		{
			if (client == null)
				return;

			if (args.Length < 3)
			{
				DisplaySyntax(client);
				return;
			}

			GameClient gameClient = null;

			// Search for the account name by client ID
			if (args[2].StartsWith("#"))
			{
				try
				{
					var sessionID = Convert.ToUInt32(args[1].Substring(1));

					gameClient = WorldMgr.GetClientFromID(sessionID);
				}
				catch
				{
					// Message: [ERROR] You have entered an invalid client ID. Type '/clientlist' to view a list of logged in clients.
					ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Ban.Err.InvalidClientID", null);
					DisplayMessage(client, "Invalid client ID");
				}
			}
			else
			{
				gameClient = WorldMgr.GetClientByPlayerName(args[2], false, false);
			}

			var accountName = gameClient != null ? gameClient.Account : DOLDB<Account>.SelectObject(DB.Column("Name").IsLike(args[2]));

			// Check to see if the specified player is online
			if (accountName == null)
			{
				// Message: [ERROR] No character is online with the name '{0}'.
				ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Command.Err.NoOnlineChar", null);
				return;
			}

			// Prevent GM from banning Admin accounts
			if (client.Account.PrivLevel < accountName.PrivLevel)
			{
				// Message: [ERROR] Your privilege level is insufficient to ban this player's account or IP address.
				ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Ban.Err.InsuffPlvl", null);
				return;
			}

			// Prevent the player from accidentally banning themselves
			if (client.Account.Name == accountName.Name)
			{
				// Message: [ERROR] You can't ban yourself!
				ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Ban.Err.CantBanSelf", null);
				return;
			}

			try
			{
				DBBannedAccount bannedAccount = new DBBannedAccount
				{
					DateBan = DateTime.Now,
					Author = client.Player.Name,
					Ip = accountName.LastLoginIP,
					Account = accountName.Name
				};

				if (args.Length >= 3)
					bannedAccount.Reason = String.Join(" ", args, 3, args.Length - 3);
				else
					bannedAccount.Reason = "No reason specified.";

				switch (args[1].ToLower())
				{
					#region Account
					// --------------------------------------------------------------------------------
					// ACCOUNT
					// '/ban account <playerName|client#> <reason>'
					// Bans the player's account indefinitely.
					// --------------------------------------------------------------------------------
					case "account":
					{
						var acctBans = DOLDB<DBBannedAccount>.SelectObjects(DB.Column("Type").IsEqualTo("A").Or(DB.Column("Type").IsEqualTo("B")).And(DB.Column("Account").IsEqualTo(accountName.Name)));

						if (acctBans.Count > 0)
						{
							// Message: [ERROR] The specified account has already been banned!
							ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Ban.Err.AlreaedyBanned", null);
							return;
						}

						bannedAccount.Type = "A";

						// Message: [SUCCESS] You have banned the account '{0}'!
						ChatUtil.SendTypeMessage(eMsg.Success, client, "GMCommands.Ban.Msg.AccountBanned", accountName.Name);

						GameClient playingclient = WorldMgr.GetClientByPlayerName(args[2], true, false);

						KickCharacter(playingclient.Player.Name);
					}
						break;
					#endregion Account
					#region IP
					// --------------------------------------------------------------------------------
					// IPADDRESS
					// '/ban ip <playerName|client#> <reason>'
					// Bans the IP address the player is currently accessing.
					// --------------------------------------------------------------------------------
					case "ip":
					{
						var ipBans = DOLDB<DBBannedAccount>.SelectObjects(DB.Column("Type").IsEqualTo("I")
							.Or(DB.Column("Type").IsEqualTo("B"))
							.And(DB.Column("Ip").IsEqualTo(accountName.LastLoginIP)));

						if (ipBans.Count > 0)
						{
							// Message: [ERROR] The account's associated IP address has already been banned!
							ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Ban.Err.IPAlreaedyBanned", null);
							return;
						}

						bannedAccount.Type = "I";

						// Message: [SUCCESS] You have banned IP address {0}!
						ChatUtil.SendTypeMessage(eMsg.Success, client, "GMCommands.Ban.Msg.IPAddrBanned", accountName.LastLoginIP);

						GameClient playingClient = WorldMgr.GetClientByPlayerName(args[2], true, false);

						KickCharacter(playingClient.Player.Name);
					}
						break;
					#endregion IP
					#region Both
					// --------------------------------------------------------------------------------
					// BOTH
					// '/ban both <playerName> <reason>'
					// Ban the specified account and its associated IP address.
					// --------------------------------------------------------------------------------
					case "both":
					{
						var acctIpBans = DOLDB<DBBannedAccount>.SelectObjects(DB.Column("Type").IsEqualTo("B")
							.And(DB.Column("Account").IsEqualTo(accountName.Name))
							.And(DB.Column("Ip").IsEqualTo(accountName.LastLoginIP)));

						if (acctIpBans.Count > 0)
						{
							// Message: [ERROR] The specified account has already been banned!
							ChatUtil.SendTypeMessage(eMsg.Error, client, "GMCommands.Ban.Err.AlreaedyBanned", null);
							return;
						}

						bannedAccount.Type = "B";

						// Message: [SUCCESS] You have banned the account '{0}' and IP address {1}!
						ChatUtil.SendTypeMessage(eMsg.Success, client, "GMCommands.Ban.Msg.AccountIPAddrBanned", accountName.Name, accountName.LastLoginIP);

						GameClient playingClient = WorldMgr.GetClientByPlayerName(args[2], true, false);

						KickCharacter(playingClient.Player.Name);
					}
						break;
					#endregion Both
					#region Default
					// --------------------------------------------------------------------------------
					// DEFAULT
					// Displays all command syntax.
					// --------------------------------------------------------------------------------
					default:
						{
							DisplaySyntax(client);
							return;
						}
					#endregion Default
				}

				GameServer.Database.AddObject(bannedAccount);

				if (log.IsInfoEnabled)
					log.Info("[INFO] Ban added for " + accountName.Name + " and " + accountName.LastLoginIP + "!");
				return;
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("[ERROR] A ban exception occurred: " + e);
			}

			// If returned here, there is an error
			DisplaySyntax(client);
		}

		/// <summary>
		/// Kicks an active playing character from the server and closes the client
		/// </summary>
		/// <param name="player">The character</param>
		private void KickCharacter(string player)
		{
			GameClient playingClient = WorldMgr.GetClientByPlayerName(player, true, false);

			if (playingClient != null)
			{
				// Message:{0} has been disconnected!
				ChatUtil.SendTypeMessage(eMsg.SysArea, playingClient, "GMCommands.Ban.Msg.BeenDisconnected", playingClient.Player.Name);

				playingClient.Out.SendPlayerQuit(true);
				playingClient.Disconnect();
			}
		}
	}
}