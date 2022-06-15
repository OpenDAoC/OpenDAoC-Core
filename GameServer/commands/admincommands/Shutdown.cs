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

using System;
using System.Threading;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using System.Collections.Generic;
using DOL.Language;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS.Commands
{
	// See the comments above 'using' about SendMessage translation IDs
	[CmdAttribute(
		// Enter '/shutdown' to list all commands of this type
		"&shutdown",
		// Message: <----- '/shutdown' Commands (plvl 3) ----->
		"AdminCommands.Header.Syntax.Shutdown",
		ePrivLevel.Admin,
		// Message: "Initiates a total shutdown of the Atlas server. The server must then be manually started by an authorized staff member, such as Clait, Fen, or Suite."
		"AdminCommands.Shutdown.Description",
		// Syntax: /shutdown command
		"AdminCommands.Shutdown.Syntax.Comm",
		// Message: "Provides additional information regarding the '/shutdown' command type."
		"AdminCommands.Shutdown.Usage.Comm",
		// Syntax: /shutdown <seconds>
		"AdminCommands.Shutdown.Syntax.Secs",
		// Message: "Schedules a manual shutdown of the server, counting down from the specified number of seconds."
		"AdminCommands.Shutdown.Usage.Secs",
		// Syntax: /shutdown on <HH>:<MM>
		"AdminCommands.Shutdown.Syntax.HrMin",
		// Message: "Schedules a manual shutdown of the server at the scheduled time (based on a 24:59 format). Atlas' server time is GMT."
		"AdminCommands.Shutdown.Usage.HrMin",
		// Syntax: /shutdown stop
		"AdminCommands.Shutdown.Syntax.Stop",
		// Message: "Cancels a scheduled server shutdown. Use this if a shutdown was triggered accidentally, is no longer needed, or lacks a qualified staff member to start the server again."
		"AdminCommands.Shutdown.Usage.Stop")]
	public class ShutdownCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private const int AUTOMATEDSHUTDOWN_CHECKINTERVALMINUTES = 15;
		private const int AUTOMATEDSHUTDOWN_HOURTOSHUTDOWN = 4; // local time
		private const int AUTOMATEDSHUTDOWN_SHUTDOWNWARNINGMINUTES = 45;

		private static long m_counter = 0;
		private static Timer m_timer;
		private static int m_time = 5;
		private static bool m_shuttingDown = false;
		private static bool m_firstAutoCheck = true;
		private static long m_currentCallbackTime = 0;

		public static long getShutdownCounter()
		{
			return m_counter;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			m_currentCallbackTime = AUTOMATEDSHUTDOWN_CHECKINTERVALMINUTES * 60 * 1000;
			m_timer = new Timer(new TimerCallback(AutomaticShutdown), null, 0, m_currentCallbackTime);
		}

		public static void AutomaticShutdown(object param)
		{
			if (m_firstAutoCheck)
			{
				// skip the first check.  This is for debugging, to make sure the timer continues to run after setting it to a small interval for testing
				m_firstAutoCheck = false;
				return;
			}

			// At least 1 hour
			if (Properties.HOURS_UPTIME_BETWEEN_SHUTDOWN <= 0) return;
			
			if (m_shuttingDown)
				return;

			TimeSpan uptime = TimeSpan.FromMilliseconds(GameServer.Instance.TickCount);

			if (uptime.TotalHours >= Properties.HOURS_UPTIME_BETWEEN_SHUTDOWN && DateTime.Now.Hour == AUTOMATEDSHUTDOWN_HOURTOSHUTDOWN)
			{
				m_counter = AUTOMATEDSHUTDOWN_SHUTDOWNWARNINGMINUTES * 60;

				//Set the timer for a 5 min callback
				m_currentCallbackTime = 5 * 60 * 1000;
				m_timer.Dispose();
				m_timer = new Timer(new TimerCallback(CountDown), null, m_currentCallbackTime, 1);

				DateTime date;
				date = DateTime.Now;
				date = date.AddSeconds(m_counter);
				var dateformat = date.ToString("HH:mm \"GMT\" zzz");

				foreach (GameClient m_client in WorldMgr.GetAllPlayingClients())
				{
					m_client.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, "Automated server restart / backup triggered. Restart in " + m_counter / 60 + " mins! (Restart at " + date.ToString("HH:mm \"GMT\" zzz") + ")");
					//m_client.Out.SendMessage("Automated server restart / backup triggered. Restart in " + m_counter / 60 + " mins! (Restart on " + date.ToString("HH:mm \"GMT\" zzz") + ")", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				}

				string msg = "Automated server restart in " + m_counter / 60 + " mins! (Restart at " + date.ToString("HH:mm \"GMT\" zzz") + ")";
				log.Warn(msg);

				// You have an IRC Bot
				//if (ServerIRC.IRCBot != null) ServerIRC.IRCBot.SendMessage(ServerIRC.CHANNEL, msg);
			}
			else
			{
				log.Info("Uptime = " + uptime.TotalHours.ToString("N1") + ", restart uptime = " + Properties.HOURS_UPTIME_BETWEEN_SHUTDOWN.ToString() +
				         " | Current hour = " + DateTime.Now.Hour.ToString() + ", restart hour = " + AUTOMATEDSHUTDOWN_HOURTOSHUTDOWN.ToString() );
			}
		}
		
		public static void CountDown(int seconds)
		{
			// Subtract the current callback time
			m_counter = seconds;
			
			// Change flage to true
			m_shuttingDown = true;
			if (m_counter <= 0)
			{
				m_timer.Dispose();
				new Thread(new ThreadStart(ShutDownServer)).Start();
			}
			else
			{
				if (m_counter > 120)
					log.Warn("Server restart in " + (int)(m_counter / 60) + " minutes!");
				else
					log.Warn("Server restart in " + m_counter + " seconds!");

				long secs = m_counter;
				long mins = secs / 60;
				long hours = mins / 60;

				string translationID = "";
				long args1 = 0;
				long args2 = 0;

				// If more than 3 hours, check hourly
				if (mins > 180)
				{
					if (mins % 60 < 15)
					{
						// Message: "A server reboot is scheduled to occur in {0} hours!"
						translationID = "AdminCommands.Shutdown.Msg.CountdownHours";
						args1 = hours;
						args2 = -1;
					}
					
					// 60 minutes between checks
					m_currentCallbackTime = 60 * 60 * 1000;
				}
				// If more than 1 hour, check every 30 minutes
				else if (mins > 60)
				{
					if (mins % 30 < 6)
					{
						// Message: "A server reboot is scheduled to occur in {0} hours and {1} minutes!"
						translationID = "AdminCommands.Shutdown.Msg.CountdownHrMn";
						
						args1 = hours;
						args2 = mins - (hours * 60);
					}
					
					// 30 minutes between checks
					m_currentCallbackTime = 30 * 60 * 1000;
				} 
				else if (mins > 15)
				{
					if (mins % 15 < 4) //every 15 mins..
					{
						// Message: "A server reboot will occur in {0} minutes!"
						translationID = "AdminCommands.Shutdown.Msg.CountdownMins";
						args1 = mins;
						args2 = -1;
					}

					// 15 minutes between checks
					m_currentCallbackTime = 15 * 60 * 1000;
				}
				else if (mins > 5)
				{
					// Message: "A server reboot will occur in {0} minutes!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownMins";
					args1 = mins;
					args2 = -1;

					// 5 minutes between checks
					m_currentCallbackTime = 5 * 60 * 1000;
				}
				else if (secs > 120)
				{
					// Message: "A server reboot will occur in {0} minutes!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownMins";
					args1 = mins;
					args2 = -1;

					// 1 minute between checks
					m_currentCallbackTime = 60 * 1000;
				}
				else if (secs > 60)
				{
					// Message: "A server reboot will occur in {0} minutes and {1} seconds!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownMnSc";
					args1 = mins;
					args2 = secs - (mins * 60);

					// 15 seconds between checks
					m_currentCallbackTime = 15 * 1000;
				}
				else if (secs > 30)
				{
					// Message: "A server reboot will occur in {0} seconds!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownSecs";
					args1 = secs;
					args2 = -1;
					
					// 10 secs between checks
					m_currentCallbackTime = 10 * 1000;
				}
				// Alert every 5 seconds below 30 seconds to reboot
				else if (secs > 10)
				{
					// Message: "Server reboot in {0} seconds! Log out now to avoid any loss of progress!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownLogoutSecs";
					args1 = secs;
					args2 = -1;
					
					// 5 seconds between checks
					m_currentCallbackTime = 5 * 1000;
				}
				else if (secs > 0)
				{
					// Message: "Server reboot in {0} seconds! Log out now to avoid any loss of progress!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownLogoutSecs";
					args1 = secs;
					args2 = -1;
					
					//5 secs between checks
					m_currentCallbackTime = 1000;
				}

				//Change the timer to the new callback time
				m_timer.Change(m_currentCallbackTime, m_currentCallbackTime);

				if (translationID != "")
				{
					foreach (GameClient client in WorldMgr.GetAllPlayingClients())
					{
						if (args2 == -1)
							ChatUtil.SendServerMessage(client, translationID, args1);
						else
							ChatUtil.SendServerMessage(client, translationID, args1, args2);
					}
				}

				if (secs <= 120 && GameServer.Instance.ServerStatus != eGameServerStatus.GSS_Closed) // 2 mins remaining
				{
					GameServer.Instance.Close();

					foreach (GameClient client in WorldMgr.GetAllPlayingClients())
					{
						// Send twice for good measure
						// Message: "The Atlas server is now closed to all incoming connections! The server will shut down in {0} seconds!"
						ChatUtil.SendDebugMessage(client, "AdminCommands.Account.Msg.ServerClosed", secs);
						ChatUtil.SendDebugMessage(client, "AdminCommands.Account.Msg.ServerClosed", secs);
					}
				}
				
				if (secs == 119 && GameServer.Instance.ServerStatus != eGameServerStatus.GSS_Closed && Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID))) // 2 mins remaining
				{
						var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);
						// var discordClient = new DiscordWebhookClient("https://discord.com/api/webhooks/928723074898075708/cyZbVefc0gc__9c2wq3DwVxOBFIT45VyK-1-z7tT_uXDd--WcHrY1lw1y9H6wPg6SEyM");
					
						var message = new DiscordMessage(
							"",
							username: "Atlas GameServer",
							avatarUrl: "https://cdn.discordapp.com/avatars/924819091028586546/656e2b335e60cb1bfaf3316d7754a8fd.webp",
							tts: false,
							embeds: new[]
							{
								new DiscordMessageEmbed(
									color: 15158332,
									description: "The server will reboot in **2 minutes** and is temporarily not accepting new incoming connections!\n Stay tuned for the patch notes.",
									thumbnail: new DiscordMessageEmbedThumbnail("https://cdn.discordapp.com/emojis/893545614942564412.webp")
								)
							}
						);

						discordClient.SendToDiscord(message);
				}
			}
		}
		public static void CountDown(object param)
		{
			// Subtract the current callback time
			m_counter -= m_currentCallbackTime / 1000;
			
			// Change flage to true
			m_shuttingDown = true;
			if (m_counter <= 0)
			{
				m_timer.Dispose();
				new Thread(new ThreadStart(ShutDownServer)).Start();
			}
			else
			{
				if (m_counter > 120)
					log.Warn("Server restart in " + (int)(m_counter / 60) + " minutes!");
				else
					log.Warn("Server restart in " + m_counter + " seconds!");

				long secs = m_counter;
				long mins = secs / 60;
				long hours = mins / 60;

				string translationID = "";
				long args1 = 0;
				long args2 = 0;

				// If more than 3 hours, check hourly
				if (mins > 180)
				{
					if (mins % 60 < 15)
					{
						// Message: "A server reboot is scheduled to occur in {0} hours!"
						translationID = "AdminCommands.Shutdown.Msg.CountdownHours";
						args1 = hours;
						args2 = -1;
					}
					
					// 60 minutes between checks
					m_currentCallbackTime = 60 * 60 * 1000;
				}
				// If more than 1 hour, check every 30 minutes
				else if (mins > 60)
				{
					if (mins % 30 < 6)
					{
						// Message: "A server reboot is scheduled to occur in {0} hours and {1} minutes!"
						translationID = "AdminCommands.Shutdown.Msg.CountdownHrMn";
						
						args1 = hours;
						args2 = mins - (hours * 60);
					}
					
					// 30 minutes between checks
					m_currentCallbackTime = 30 * 60 * 1000;
				} 
				else if (mins > 15)
				{
					if (mins % 15 < 4) //every 15 mins..
					{
						// Message: "A server reboot will occur in {0} minutes!"
						translationID = "AdminCommands.Shutdown.Msg.CountdownMins";
						args1 = mins;
						args2 = -1;
					}

					// 15 minutes between checks
					m_currentCallbackTime = 15 * 60 * 1000;
				}
				else if (mins > 5)
				{
					// Message: "A server reboot will occur in {0} minutes!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownMins";
					args1 = mins;
					args2 = -1;

					// 5 minutes between checks
					m_currentCallbackTime = 5 * 60 * 1000;
				}
				else if (secs > 120)
				{
					// Message: "A server reboot will occur in {0} minutes!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownMins";
					args1 = mins;
					args2 = -1;

					// 1 minute between checks
					m_currentCallbackTime = 60 * 1000;
				}
				else if (secs > 60)
				{
					// Message: "A server reboot will occur in {0} minutes and {1} seconds!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownMnSc";
					args1 = mins;
					args2 = secs - (mins * 60);

					// 15 seconds between checks
					m_currentCallbackTime = 15 * 1000;
				}
				else if (secs > 30)
				{
					// Message: "A server reboot will occur in {0} seconds!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownSecs";
					args1 = secs;
					args2 = -1;
					
					// 10 secs between checks
					m_currentCallbackTime = 10 * 1000;
				}
				// Alert every 5 seconds below 30 seconds to reboot
				else if (secs > 10)
				{
					// Message: "Server reboot in {0} seconds! Log out now to avoid any loss of progress!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownLogoutSecs";
					args1 = secs;
					args2 = -1;
					
					// 5 seconds between checks
					m_currentCallbackTime = 5 * 1000;
				}
				else if (secs > 0)
				{
					// Message: "Server reboot in {0} seconds! Log out now to avoid any loss of progress!"
					translationID = "AdminCommands.Shutdown.Msg.CountdownLogoutSecs";
					args1 = secs;
					args2 = -1;
					
					//5 secs between checks
					m_currentCallbackTime = 1000;
				}

				//Change the timer to the new callback time
				m_timer.Change(m_currentCallbackTime, m_currentCallbackTime);

				if (translationID != "")
				{
					foreach (GameClient client in WorldMgr.GetAllPlayingClients())
					{
						if (args2 == -1)
							ChatUtil.SendServerMessage(client, translationID, args1);
						else
							ChatUtil.SendServerMessage(client, translationID, args1, args2);
					}
				}

				if (secs <= 120 && GameServer.Instance.ServerStatus != eGameServerStatus.GSS_Closed) // 2 mins remaining
				{
					GameServer.Instance.Close();

					foreach (GameClient client in WorldMgr.GetAllPlayingClients())
					{
						// Send twice for good measure
						// Message: "The Atlas server is now closed to all incoming connections! The server will shut down in {0} seconds!"
						ChatUtil.SendDebugMessage(client, "AdminCommands.Account.Msg.ServerClosed", secs);
						ChatUtil.SendDebugMessage(client, "AdminCommands.Account.Msg.ServerClosed", secs);
					}
				}
				
				if (secs == 119 && GameServer.Instance.ServerStatus != eGameServerStatus.GSS_Closed && Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID))) // 2 mins remaining
				{
						var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);
						// var discordClient = new DiscordWebhookClient("https://discord.com/api/webhooks/928723074898075708/cyZbVefc0gc__9c2wq3DwVxOBFIT45VyK-1-z7tT_uXDd--WcHrY1lw1y9H6wPg6SEyM");
					
						var message = new DiscordMessage(
							"",
							username: "Atlas GameServer",
							avatarUrl: "https://cdn.discordapp.com/avatars/924819091028586546/656e2b335e60cb1bfaf3316d7754a8fd.webp",
							tts: false,
							embeds: new[]
							{
								new DiscordMessageEmbed(
									color: 15158332,
									description: "The server will reboot in **2 minutes** and is temporarily not accepting new incoming connections!\n Stay tuned for the patch notes.",
									thumbnail: new DiscordMessageEmbedThumbnail("https://cdn.discordapp.com/emojis/893545614942564412.webp")
								)
							}
						);

						discordClient.SendToDiscord(message);
				}
			}
		}
		
		/// <summary>
		/// Shuts down all server components
		/// </summary>
		public static void ShutDownServer()
		{
			if (GameServer.Instance.IsRunning)
			{
				GameServer.Instance.Stop();
				log.Info("Executed Atlas server shutdown!");
				Thread.Sleep(2000);
				Environment.Exit(0);
			}
		}

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists '/shutdown' commands' syntax (see '&shutdown' section above)
				DisplaySyntax(client);
				return;
			}
			
			DateTime date;
			//if (m_counter > 0) return 0;
			
			// Player executing command
			GamePlayer user = client.Player;

			switch (args[1])
			{
				#region Commmand
				// Provides additional information regarding the '/shutdown' command type
				// Syntax: /shutdown command
				// Args:   /shutdown args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "command":
				{
					// Displays dialog with information
					var info = new List<string>();
					info.Add(" ");
					// Message: "----- Implications of a Shutdown -----"
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.Shutdown"));
					info.Add(" ");
					// Message: "The '/shutdown' command triggers a countdown timer that prevents any new incoming connections (when fewer than 2 minutes remain) and sends the exit code, stopping all DOL-related activity. This should not be confused with the '/serverreboot' command (which presently does not work)."
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Comm.Desc1"));
					info.Add(" ");
					// Message: "Rebooting Atlas is a team effort. Bringing the server back online is performed manually by Clait, Fen, or Suite. Please make sure someone is prepared to run the server once the shutdown is complete. Do not use these commands unless instructed."
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Comm.Desc2"));
					info.Add(" ");
					// Message: "----- Additional Info -----"
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.MoreInfo"));
					info.Add(" ");
					// Message: "For more information regarding the '/shutdown' command type, see the GM Commands Library on the Atlas Developers forum."
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Comm.Desc3"));
		
					client.Out.SendCustomTextWindow("Using the '/shutdown' Command Type", info);
					return;
				}
				#endregion Command

				#region Stop
				// Cancels a server shutdown
				// Syntax: /shutdown stop
				// Args:   /shutdown args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "stop":
				{
					// If the server is counting down
					if (m_counter != 0)
					{
						// Clear countdown and change shutdown flag to 'false'
						m_timer.Dispose();
						m_counter = 0;
						m_shuttingDown = false;
						
						// Send this message to player executing stop command
						// Message: "You have stopped the server shutdown process!"
						ChatUtil.SendDebugMessage(client, "AdminCommands.Shutdown.Msg.YouCancel", null);
						
						// Send message to all players letting them know the shutdown isn't occurring
						foreach (GameClient playingClient in WorldMgr.GetAllPlayingClients())
						{
							// Message: "{0} stopped the server shutdown!"
							ChatUtil.SendDebugMessage(playingClient, "AdminCommands.Shutdown.Msg.StaffCancel", user.Name);
							// Message: "The server restart has been canceled! Please stand by for additional information from Atlas staff."
							ChatUtil.SendServerMessage(playingClient, "AdminCommands.Shutdown.Msg.ShutdownEnd", null);
						}

						// If server status is closed (< 2 min to shutdown)
						if (GameServer.Instance.ServerStatus == eGameServerStatus.GSS_Closed)
						{
							// Allow incoming connections
							GameServer.Instance.Open();
							// Message: "The Atlas server is now open and accepting incoming connections!"
							ChatUtil.SendDebugMessage(client, "AdminCommands.Shutdown.Msg.ServerOpen", null);
							log.Info("Shutdown aborted. Server now accepting incoming connections!");
						}
						else
						{
							log.Info("Shutdown aborted. Server still accepting incoming connections!");
						}
						
						if (Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID)))
						{

							var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);
							// var discordClient = new DiscordWebhookClient("https://discord.com/api/webhooks/928723074898075708/cyZbVefc0gc__9c2wq3DwVxOBFIT45VyK-1-z7tT_uXDd--WcHrY1lw1y9H6wPg6SEyM");
					
							var message = new DiscordMessage(
								"",
								username: "Atlas GameServer",
								avatarUrl: "https://cdn.discordapp.com/avatars/924819091028586546/656e2b335e60cb1bfaf3316d7754a8fd.webp",
								tts: false,
								embeds: new[]
								{
									new DiscordMessageEmbed(
										color: 3066993,
										description: "The server restart has been cancelled.\nPlease stand by for additional information from Atlas team.",
										thumbnail: new DiscordMessageEmbedThumbnail("https://cdn.discordapp.com/emojis/865577034087923742.png")
									)
								}
							);

							discordClient.SendToDiscord(message);
						}
						
					}
					// If no countdown is detected
					else
					{
						// Message: "No server shutdown is scheduled currently!"
						ChatUtil.SendErrorMessage(client, "AdminCommands.Shutdown.Err.NoShutdown", null);
					}
					return;
				}
				#endregion Stop

				#region On HH:MM
				// Schedules a server shutdown for a specific time of the day (dependent on the server time)
				// Syntax: /shutdown on <HH>:<MM>
				// Args:   /shutdown args[1] args[2]
				// See the comments above 'using' about SendMessage translation IDs
				case "on":
				{
					// Check for '/shutdown on HH:MM'
					// If '/shutdown on' + args[2]
					if (args.Length == 3)
					{
						// Require separator between HH and MM
						string[] shutdownsplit = args[2].Split(':');

						if (args[2].Contains(':') == false || shutdownsplit[0].Length is < 1 or > 2 || shutdownsplit[1].Length is < 2 or > 2)
						{
							// Lists '/shutdown on' command syntax (see '&shutdown' section above)
							// Message: "<----- '/shutdown' Commands (plvl 3) ----->"
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Shutdown", null);
							// Message: "Use the following syntax for this command:"
							ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxDesc", null);
							// Syntax: /shutdown on <HH>:<MM>
							ChatUtil.SendSyntaxMessage(client, "AdminCommands.Shutdown.Syntax.HrMin", null);
							// Message: "Schedules a manual shutdown of the server at the scheduled time (based on a 24:59 format). Atlas' server time is GMT."
							ChatUtil.SendCommMessage(client, "AdminCommands.Shutdown.Usage.HrMin", null);
							return;
						}

						int hour = Convert.ToInt32(shutdownsplit[0]);
						int min = Convert.ToInt32(shutdownsplit[1]);
						// found next date with hour:min

						date = DateTime.Now;

						if ((date.Hour > hour) ||
						    (date.Hour == hour && date.Minute > min)
						   )
							date = new DateTime(date.Year, date.Month, date.Day + 1);

						if (date.Minute > min)
							date = new DateTime(date.Year, date.Month, date.Day, date.Hour + 1, 0, 0);

						date = date.AddHours(hour - date.Hour);
						date = date.AddMinutes(min - date.Minute + 1);
						date = date.AddSeconds(- date.Second);

						m_counter = (date.ToFileTime() - DateTime.Now.ToFileTime()) / TimeSpan.TicksPerSecond;

						if (m_counter < 60) m_counter = 60;
					}
					// If '/shutdown on', but something else is wrong with command syntax
					else
					{
						// Lists '/shutdown on' command syntax (see '&shutdown' section above)
						// Message: "<----- '/shutdown' Commands (plvl 3) ----->"
						ChatUtil.SendSyntaxMessage(client, "AdminCommands.Header.Syntax.Shutdown", null);
						// Message: "Use the following syntax for this command:"
						ChatUtil.SendCommMessage(client, "AdminCommands.Command.SyntaxDesc", null);
						// Syntax: /shutdown on <HH>:<MM>
						ChatUtil.SendSyntaxMessage(client, "AdminCommands.Shutdown.Syntax.HrMin", null);
						// Message: "Schedules a manual shutdown of the server at the scheduled time (based on a 24:59 format). Atlas' server time is GMT."
						ChatUtil.SendCommMessage(client, "AdminCommands.Shutdown.Usage.HrMin", null);
						return;
					}

					break;
				}
				#endregion On HH:MM
				
				#region Minutes
				// Schedules a server shutdown in the specified number of seconds
				// Syntax: /shutdown <seconds>
				// Args:   /shutdown args[1]
				// See the comments above 'using' about SendMessage translation IDs
				default:
				{
					// Check for '/shutdown <seconds>'
					if (args.Length == 2)
					{
						// Try to convert args[1] into seconds (minutes * 60)
						try
						{
							m_counter = System.Convert.ToInt32(args[1]);
						}
						// If an unexpected value for args[1]
						catch (Exception)
						{
							// Lists '/shutdown' commands' syntax (see '&shutdown' section above)
							DisplaySyntax(client);
							return;
						}
						// Require a value equal to or between 10 and 43200 (10 seconds to 6 hours)
						if (m_counter is <= 9 or >= 43200)
						{
							// Message: "A server shutdown could not be initiated! Enter a value between '1' (i.e., 1 minute) and '720' (i.e., 12 hours) to start the shutdown counter. Otherwise, schedule a shutdown using '/shutdown on <HH>:<MM>'."
							ChatUtil.SendErrorMessage(client, "AdminCommands.Shutdown.Err.WrongNumber", null);
							return;
						}
					}
					// Any other syntax issues are caught here
					else
					{
						// Lists '/shutdown' commands' syntax (see '&shutdown' section above)
						DisplaySyntax(client);
						return;
					}
					break;
				}
				#endregion Minutes
			}
			
			if (m_counter % 5 != 0)
				m_counter = (m_counter / 5 * 5);

			if (m_counter == 0)
				m_counter = m_time * 60;

			date = DateTime.Now;
			date = date.AddSeconds(m_counter);
			
			// Message: "A full server reboot will occur in {0} minutes!"
			string msg = "AdminCommands.Shutdown.Msg.CountdownMins";
			bool popup = (m_counter / 60) < 60;
			long counter = m_counter / 60;
			
			foreach (GameClient m_client in WorldMgr.GetAllPlayingClients())
			{
				if (popup)
				{
					// Displays dialog with information
					var shutdown = new List<string>();
					
					shutdown.Add(" ");
					// Message: "----- ATTENTION -----"
					shutdown.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.Attention"));
					shutdown.Add(" ");
					// Message: "A server reboot has been scheduled to occur at {1}. The Atlas server will then be unavailable temporarily for maintenance."
					shutdown.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Msg.ScheduledShutdown", date.ToString("HH:mm \"GMT\" zzz")));
					shutdown.Add(" ");
					// Message: "It is recommended that players log out prior to the reboot to ensure RvR kills, ROG drops, and other progress is not lost."
					shutdown.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Msg.PlanLogout"));
				
					// Send the above messages in a dialog
					m_client.Out.SendCustomTextWindow("Server Reboot Scheduled", shutdown);
				}

				// Message: "ATTENTION: A server shutdown will take place in {0} minutes! The shutdown is scheduled at {1}."
				ChatUtil.SendServerMessage(m_client, "AdminCommands.Shutdown.Msg.AttentionShutdown", m_counter / 60, date.ToString("HH:mm \"GMT\" zzz"));
			}

			if (Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID)))
			{

				var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);
				// var discordClient = new DiscordWebhookClient("https://discord.com/api/webhooks/928723074898075708/cyZbVefc0gc__9c2wq3DwVxOBFIT45VyK-1-z7tT_uXDd--WcHrY1lw1y9H6wPg6SEyM");
					
				var message = new DiscordMessage(
					"",
					username: "Atlas GameServer",
					avatarUrl: "https://cdn.discordapp.com/avatars/924819091028586546/656e2b335e60cb1bfaf3316d7754a8fd.webp",
					tts: false,
					embeds: new[]
					{
						new DiscordMessageEmbed(
							color: 15844367,
							description: $"A server restart has been scheduled for {date:HH:mm \"GMT\" zzz}",
							thumbnail: new DiscordMessageEmbedThumbnail("https://cdn.discordapp.com/attachments/879754382231613451/959414859932532756/unknown.png")
						)
					}
				);

				discordClient.SendToDiscord(message);
			}

			log.Warn(msg);

			m_currentCallbackTime = 0;
			m_timer?.Dispose();
			m_timer = new Timer(new TimerCallback(CountDown), null, 0, 15000);
		}
	}
}