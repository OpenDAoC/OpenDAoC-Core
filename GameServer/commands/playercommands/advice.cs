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
using DOL.GS.API;
using DOL.Language;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&advice",
		 new [] { "&adv" },
		ePrivLevel.Player,
		// Displays next to the command when '/cmd' is entered
		"Lists all flagged Advisors, sends advisors questions, and sends messages to the Advice channel.",
		// Message: '/adv <message>' - Sends a message to the Advice channel.
		"PLCommands.Advice.Syntax.AdvChannel",
		// Message: '/advice' - Lists all online Advisors.
		"PLCommands.Advice.Syntax.Advice",
		// '/advisor' - Flags your character as an Advisor (<ADV>) to indicate that you are willing to answer new players' questions.
		"PLCommands.Advisor.Syntax.Advisor",
		// Message: '/advisor <advisorName> <message>' - Directly messages an Advisor with your question.
		"PLCommands.Advice.Syntax.SendAdvisor")]
	public class AdviceCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private const string advTimeoutString = "lastAdviceTick";
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player.IsMuted)
			{
				// Message: You have been muted by Atlas staff and are not allowed to speak in this channel.
				ChatUtil.SendGMMessage(client, "GMCommands.Mute.Err.NoSpeakChannel", null);
				return;
			}

			if (IsSpammingCommand(client.Player, "advice") || IsSpammingCommand(client.Player, "adv"))
				return;
			
			var lastAdviceTick = client.Player.TempProperties.getProperty<long>(advTimeoutString);
			var slowModeLength = Properties.ADVICE_SLOWMODE_LENGTH * 1000;
			
			if ((GameLoop.GameLoopTime - lastAdviceTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
			{
				// Message: You must wait {0} seconds before using this command again.
				ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.Wait", 60 - (GameLoop.GameLoopTime - lastAdviceTick) / 1000);
				return;
			}

			string msg = "";
			if (args.Length >= 2)
			{
				for (int i = 1; i < args.Length; ++i)
				{
					msg += args[i] + " ";
				}
			}
			if (args.Length == 1)
			{
				int total = 0;
				TimeSpan showPlayed = TimeSpan.FromSeconds(client.Player.PlayedTime);
				
				// Message: The following players are flagged as Advisors:
				ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.TheFollowing", null);
				
				foreach (GameClient playerClient in WorldMgr.GetAllClients())
				{
					if (playerClient.Player == null) continue;
					if (playerClient.Player.Advisor &&
					   ((playerClient.Player.Realm == client.Player.Realm && playerClient.Player.IsAnonymous == false) ||
					   client.Account.PrivLevel > 1))
					{
						total++;
						if (playerClient.Player.ClassNameFlag == false && playerClient.Player.CraftTitle.GetValue(playerClient.Player, client.Player).StartsWith("Legendary"))
						{
							// Message: {0}) {1}, Level {2} {3} ({4} days, {5} hours, {6} minutes played)
							// Example: 1) Fen, Level 43 Legendary Grandmaster Basic Crafter (1 days, 3 hours, 31 minutes played)
							ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.Result", total, playerClient.Player.Name, playerClient.Player.Level, playerClient.Player.CraftTitle.GetValue(playerClient.Player, client.Player), showPlayed.Days, showPlayed.Hours, showPlayed.Minutes);
						}
						else
							// Message: "{0}) {1}, Level {2} {3} ({4} days, {5} hours, {6} minutes played)"
							// Example: 1) Kelt, Level 50 Bard (14 days, 3 hours, 31 minutes played)
							ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.Result", total, playerClient.Player.Name, playerClient.Player.Level, playerClient.Player.CharacterClass.Name, showPlayed.Days, showPlayed.Hours, showPlayed.Minutes);
					}
				}
				if (total == 1)
					// Message: There is 1 Advisor online!
					ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.1AdvisorOn", null);
				else
					// Message: There are {0} Advisors online!
					ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.AdvisorsOn", total);
				return;
			}
			foreach (GameClient playerClient in WorldMgr.GetAllClients())
			{
				if (playerClient.Player == null) continue;
				if ((playerClient.Player.Realm == client.Player.Realm ||
					playerClient.Account.PrivLevel > 1) && !playerClient.Player.IsIgnoring(client.Player))
					// Message: [ADVICE {0}] {1}: {2}
					ChatUtil.SendAdviceMessage(playerClient, "Social.SendAdvice.Msg.Channel", getRealmString(client.Player.Realm), client.Player.Name, msg);

			}
			if (Properties.DISCORD_ACTIVE) WebhookMessage.LogChatMessage(client.Player, eChatType.CT_Advise, msg);

			if (client.Account.PrivLevel == 1)
			{
				client.Player.TempProperties.setProperty(advTimeoutString, GameLoop.GameLoopTime);
			}

		}

		public string getRealmString(eRealm Realm)
		{
			switch (Realm)
			{
				case eRealm.Albion: return "ALB";
				case eRealm.Midgard: return "MID";
				case eRealm.Hibernia: return "HIB";
				default: return "NONE";
			}
		}
	}
}