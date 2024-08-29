using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
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
		// Message: "Initiates a total shutdown of the server."
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
		// Message: "Schedules a manual shutdown of the server at the scheduled time (based on a 24:59 format)."
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
				string msg = $"Automated server restart in {m_counter / 60} mins! (Restart at {date:HH:mm \"GMT\" zzz})";

				foreach (GamePlayer player in ClientService.GetPlayers())
					player.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, msg);

				log.Warn(msg);
			}
			else
				log.Info($"Uptime = {uptime.TotalHours:N1}, restart uptime = {Properties.HOURS_UPTIME_BETWEEN_SHUTDOWN} | current hour = {DateTime.Now.Hour}, restart hour = {AUTOMATEDSHUTDOWN_HOURTOSHUTDOWN}");
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

				string translationID = string.Empty;
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

				if (translationID != string.Empty)
				{
					foreach (GamePlayer player in ClientService.GetPlayers())
					{
						if (args2 == -1)
							ChatUtil.SendServerMessage(player.Client, translationID, args1);
						else
							ChatUtil.SendServerMessage(player.Client, translationID, args1, args2);
					}
				}

				if (secs <= 120 && GameServer.Instance.ServerStatus != EGameServerStatus.GSS_Closed) // 2 mins remaining
				{
					GameServer.Instance.Close();

					foreach (GamePlayer player in ClientService.GetPlayers())
					{
						// Message: "The server is now closed to all incoming connections! The server will shut down in {0} seconds!"
						ChatUtil.SendDebugMessage(player.Client, "AdminCommands.Account.Msg.ServerClosed", secs);
					}
				}
				
				if (secs == 119 && GameServer.Instance.ServerStatus != EGameServerStatus.GSS_Closed && Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID))) // 2 mins remaining
				{
						var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);

						var message = new DiscordMessage(
							"",
							username: "Game Server",
							avatarUrl: "",
							tts: false,
							embeds: new[]
							{
								new DiscordMessageEmbed(
									color: 15158332,
									description: "The server will reboot in **2 minutes** and is temporarily not accepting new incoming connections!",
									thumbnail: new DiscordMessageEmbedThumbnail("")
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

				string translationID = string.Empty;
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

				if (translationID != string.Empty)
				{
					foreach (GamePlayer player in ClientService.GetPlayers())
					{
						if (args2 == -1)
							ChatUtil.SendServerMessage(player.Client, translationID, args1);
						else
							ChatUtil.SendServerMessage(player.Client, translationID, args1, args2);
					}
				}

				if (secs <= 120 && GameServer.Instance.ServerStatus != EGameServerStatus.GSS_Closed) // 2 mins remaining
				{
					GameServer.Instance.Close();

					foreach (GamePlayer player in ClientService.GetPlayers())
					{
						// Message: "The server is now closed to all incoming connections! The server will shut down in {0} seconds!"
						ChatUtil.SendDebugMessage(player.Client, "AdminCommands.Account.Msg.ServerClosed", secs);
					}
				}
				
				if (secs == 119 && GameServer.Instance.ServerStatus != EGameServerStatus.GSS_Closed && Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID))) // 2 mins remaining
				{
						var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);

						var message = new DiscordMessage(
							"",
							username: "Game Server",
							avatarUrl: "",
							tts: false,
							embeds: new[]
							{
								new DiscordMessageEmbed(
									color: 15158332,
									description: "The server will reboot in **2 minutes** and is temporarily not accepting new incoming connections!",
									thumbnail: new DiscordMessageEmbedThumbnail("")
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
				log.Info("Executed server shutdown!");
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
					// Message: "----- Additional Info -----"
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.MoreInfo"));
		
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
						foreach (GamePlayer player in ClientService.GetPlayers())
						{
							// Message: "{0} stopped the server shutdown!"
							ChatUtil.SendDebugMessage(player.Client, "AdminCommands.Shutdown.Msg.StaffCancel", user.Name);
							// Message: "The server restart has been canceled! Please stand by for additional information."
							ChatUtil.SendServerMessage(player.Client, "AdminCommands.Shutdown.Msg.ShutdownEnd", null);
						}

						// If server status is closed (< 2 min to shutdown)
						if (GameServer.Instance.ServerStatus == EGameServerStatus.GSS_Closed)
						{
							// Allow incoming connections
							GameServer.Instance.Open();
							// Message: "The server is now open and accepting incoming connections!"
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
								username: "Game Server",
								avatarUrl: "",
								tts: false,
								embeds: new[]
								{
									new DiscordMessageEmbed(
										color: 3066993,
										description: "The server restart has been cancelled.",
										thumbnail: new DiscordMessageEmbedThumbnail("")
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
							// Message: "Schedules a manual shutdown of the server at the scheduled time (based on a 24:59 format)."
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
						// Message: "Schedules a manual shutdown of the server at the scheduled time (based on a 24:59 format)."
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

			bool popup = (m_counter / 60) < 60;
			long counter = m_counter / 60;
			
			foreach (GamePlayer player in ClientService.GetPlayers())
			{
				if (popup)
				{
					// Displays dialog with information
					var shutdown = new List<string>();
					
					shutdown.Add(" ");
					// Message: "----- ATTENTION -----"
					shutdown.Add(LanguageMgr.GetTranslation(client.Account.Language, "Dialog.Header.Content.Attention"));
					shutdown.Add(" ");
					// A server reboot has been scheduled to occur at {0}. The server will then be temporarily unavailable.
					shutdown.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Msg.ScheduledShutdown", date.ToString("HH:mm \"GMT\" zzz")));
					shutdown.Add(" ");
					// Message: "It is recommended that players log out prior to the reboot to ensure RvR kills, ROG drops, and other progress is not lost."
					shutdown.Add(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Shutdown.Msg.PlanLogout"));
				
					// Send the above messages in a dialog
					player.Out.SendCustomTextWindow("Server Reboot Scheduled", shutdown);
				}

				// Message: "ATTENTION: A server shutdown will take place in {0} minutes! The shutdown is scheduled at {1}."
				ChatUtil.SendServerMessage(player.Client, "AdminCommands.Shutdown.Msg.AttentionShutdown", m_counter / 60, date.ToString("HH:mm \"GMT\" zzz"));
			}

			if (Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID)))
			{

				var discordClient = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);

				var message = new DiscordMessage(
					"",
					username: "Game Server",
					avatarUrl: "",
					tts: false,
					embeds: new[]
					{
						new DiscordMessageEmbed(
							color: 15844367,
							description: $"A server restart has been scheduled for {date:HH:mm \"GMT\" zzz}",
							thumbnail: new DiscordMessageEmbedThumbnail("")
						)
					}
				);

				discordClient.SendToDiscord(message);
			}

			m_currentCallbackTime = 0;
			m_timer?.Dispose();
			m_timer = new Timer(new TimerCallback(CountDown), null, 0, 15000);
		}
	}
}
