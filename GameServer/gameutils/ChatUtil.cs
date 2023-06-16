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
using System.Collections.Generic;
using System.Collections.Immutable;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// ChatUtil for Sending Message to Players
	/// </summary>
	public static class ChatUtil
	{
		/// <summary>
		/// Dictionary for assigning eChatType and eChatLoc based on eMsg type.
		/// </summary>
		private static ImmutableDictionary<eMsg, (eChatType, eChatLoc)> table = new Dictionary<eMsg, (eChatType, eChatLoc)>
        {
            { eMsg.None, (eChatType.CT_Staff, eChatLoc.CL_SystemWindow) },
	        { eMsg.Advice, (eChatType.CT_Advise, eChatLoc.CL_ChatWindow) }, // Advice channel messages
	        { eMsg.Alliance, (eChatType.CT_Alliance, eChatLoc.CL_ChatWindow) }, // Guild alliance messages
            { eMsg.Announce, (eChatType.CT_Staff, eChatLoc.CL_ChatWindow) }, // Server-wide announcement messages
            { eMsg.Battlegroup, (eChatType.CT_BattleGroup, eChatLoc.CL_ChatWindow) }, // Standard battlegroup channel messages
            { eMsg.BGLeader, (eChatType.CT_BattleGroupLeader, eChatLoc.CL_ChatWindow) }, // Battlegroup leader channel messages
            { eMsg.Broadcast, (eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow) }, // Broadcast channel messages
            { eMsg.CenterSys, (eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_SystemWindow) }, // Center screen and system window
            { eMsg.Chat, (eChatType.CT_Chat, eChatLoc.CL_ChatWindow) }, // Chat channel messages
            { eMsg.Command, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // Command general information, "It is recommended..."
            { eMsg.CmdDesc, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // Description of command type, "Manually executes a custom code."
            { eMsg.CmdHeader, (eChatType.CT_Important, eChatLoc.CL_SystemWindow) }, // Command type header, "<----- '/account' Commands (plvl 3) ----->"
            { eMsg.CmdSyntax, (eChatType.CT_Important, eChatLoc.CL_SystemWindow) }, // Command syntax, "/account accountname <characterName>"
            { eMsg.CmdUsage, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // Command description, "Deletes the specified account, along with any associated characters."
            { eMsg.DamageAddSh, (eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow) }, // Damage add (DA) & damage shield (DS)
            { eMsg.Damaged, (eChatType.CT_Damaged, eChatLoc.CL_SystemWindow) },// You damage target
            { eMsg.Debug, (eChatType.CT_Staff, eChatLoc.CL_SystemWindow) }, // Admin debug messages
            { eMsg.Emote, (eChatType.CT_Emote, eChatLoc.CL_SystemWindow) }, // Emote-formatted messages
            { eMsg.EmoteSysOthers, (eChatType.CT_Emote, eChatLoc.CL_SystemWindow) }, // Emote-formatted messages sent to all other players except the originating player
            { eMsg.Error, (eChatType.CT_Important, eChatLoc.CL_SystemWindow) }, // Incorrect outputs or problems with command syntax
            { eMsg.Expires, (eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow) }, // Spell expires
            { eMsg.Group, (eChatType.CT_Group, eChatLoc.CL_ChatWindow) }, // Group channel messages
            { eMsg.Guild, (eChatType.CT_Guild, eChatLoc.CL_ChatWindow) }, // Guild channel messages
            { eMsg.Help, (eChatType.CT_Help, eChatLoc.CL_ChatWindow) }, // System help
            { eMsg.Important, (eChatType.CT_Important, eChatLoc.CL_SystemWindow) }, // Important system messages
            { eMsg.KilledByAlb, (eChatType.CT_KilledByAlb, eChatLoc.CL_SystemWindow) }, // Alb killspam
            { eMsg.KilledByHib, (eChatType.CT_KilledByHib, eChatLoc.CL_SystemWindow) }, // Hib killspam
            { eMsg.KilledByMid, (eChatType.CT_KilledByMid, eChatLoc.CL_SystemWindow) }, // Mid killspam
            { eMsg.LFG, (eChatType.CT_LFG, eChatLoc.CL_ChatWindow) }, // LFG channel
            { eMsg.Loot, (eChatType.CT_Loot, eChatLoc.CL_SystemWindow) }, // Item drops
            { eMsg.Merchant, (eChatType.CT_Merchant, eChatLoc.CL_SystemWindow) }, // Merchant messages
            { eMsg.Message, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // System messages conveying information
            { eMsg.Missed, (eChatType.CT_Missed, eChatLoc.CL_SystemWindow) }, // Missed combat
            { eMsg.Officer, (eChatType.CT_Officer, eChatLoc.CL_ChatWindow) }, // Officer channel
            { eMsg.OthersCombat, (eChatType.CT_OthersCombat, eChatLoc.CL_SystemWindow) }, // Other's combat messages
            { eMsg.PlayerDied, (eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow) }, // Player died
            { eMsg.PlayerDiedOthers, (eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow) }, // Player death message sent to other nearby players
            { eMsg.Pulse, (eChatType.CT_SpellPulse, eChatLoc.CL_SystemWindow) }, // Spell pulse
            { eMsg.Resisted, (eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow) }, // Spell resisted
            { eMsg.Say, (eChatType.CT_Say, eChatLoc.CL_ChatWindow) }, // GameLiving say
            { eMsg.ScreenCenter, (eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow) }, // Center of screen
            { eMsg.Send, (eChatType.CT_Send, eChatLoc.CL_ChatWindow) }, // Private messages
            { eMsg.Server, (eChatType.CT_Staff, eChatLoc.CL_SystemWindow) }, // Server-wide alerts
            { eMsg.Skill, (eChatType.CT_Skill, eChatLoc.CL_SystemWindow) }, // Skill-related messages
            { eMsg.Spell, (eChatType.CT_Spell, eChatLoc.CL_SystemWindow) }, // Spell casting and effects
            { eMsg.Staff, (eChatType.CT_Staff, eChatLoc.CL_ChatWindow) }, // Admin/GM communications with players
            { eMsg.Success, (eChatType.CT_Important, eChatLoc.CL_SystemWindow) }, // Important command execution messages
            { eMsg.SysArea, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // System messages sent to a general AoE space
            { eMsg.SysOthers, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // System messages sent to all nearby players, except the target
            { eMsg.System, (eChatType.CT_System, eChatLoc.CL_SystemWindow) }, // Standard system messages
            { eMsg.Team, (eChatType.CT_Staff, eChatLoc.CL_ChatWindow) }, // Team channel
            { eMsg.Trade, (eChatType.CT_Trade, eChatLoc.CL_ChatWindow) }, // Trade channel
            { eMsg.Yell, (eChatType.CT_Help, eChatLoc.CL_ChatWindow) }, // Yelling communication
            { eMsg.YouDied, (eChatType.CT_YouDied, eChatLoc.CL_SystemWindow) }, // You died messages
            { eMsg.YouHit, (eChatType.CT_YouHit, eChatLoc.CL_SystemWindow) }, // You hit a target messages
            { eMsg.YouWereHit, (eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow) }, // GameLiving hit you messages
            { eMsg.Failed, (eChatType.CT_System, eChatLoc.CL_SystemWindow) } // Failed system messages
        }.ToImmutableDictionary();

		/// <summary>
        /// Used to send translated messages
        /// </summary>
        /// <param name="type">Determines the eChatType and eChatLoc to use for the message.</param>
        /// <param name="target">The target client receiving the message (e.g., "target.Client")</param>
        /// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
        /// <param name="args">Any argument values to include in the message, such as "client.Player.Name" (if no args, then use "null")</param>
        /// <note>Please paste the translation ID's associated message (English) into a comment above this method.</note>
        /// <example>ChatUtil.SendTypeMessage(eMsg.Command, client, "AdminCommands.Account.Description", null);</example>
        /// <comment>To perform message changes, reference the '.txt' files located in 'GameServer > language > EN'. Please remember, if you change the translation ID value, check all other language folders (e.g., DE, FR, ES, etc.) to ensure all translated strings correctly reflect the new ID.</comment>
        public static void SendTypeMessage(eMsg type, GamePlayer target, string translationID, params object[] args)
        {
            // See example above for formatting
            var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

            // Announcement messages sent using '/announce'
            if (table.TryGetValue(eMsg.Announce, out var announcement))
            {
	            target.Client.Out.SendMessage(translatedMsg, announcement.Item1, announcement.Item2);
	            target.Client.Out.SendMessage(translatedMsg, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            }
            // Server-wide alerts, send twice for good measure
            else if (table.TryGetValue(eMsg.Server, out var server))
            {
	            target.Client.Out.SendMessage(translatedMsg, server.Item1, server.Item2);
	            target.Client.Out.SendMessage(translatedMsg, server.Item1, server.Item2);
            }
            // Admin debugging messages when troubleshooting behaviors/commands/activities in-game
            else if (table.TryGetValue(eMsg.Debug, out var debug) && target.Client.Account.PrivLevel > (int)ePrivLevel.GM)
	            target.Client.Out.SendMessage(translatedMsg, debug.Item1, debug.Item2);
            // Emote-formatted messages sent to all other players, excluding the originating player
            else if (table.TryGetValue(eMsg.EmoteSysOthers, out var emote))
	            Message.SystemToOthers(target, translatedMsg, emote.Item1);
            // Player death message sent to other nearby players
            else if (table.TryGetValue(eMsg.PlayerDiedOthers, out var diedOthers))
	            Message.SystemToOthers2(target, diedOthers.Item1, translationID, args);
            // System messages sent to a general AoE space
            else if (table.TryGetValue(eMsg.SysArea, out var sysArea))
	            Message.SystemToArea(target, translatedMsg, sysArea.Item1, target);
            // System messages sent to all nearby players, except the target
            else if (table.TryGetValue(eMsg.SysOthers, out var sysOthers))
	            Message.SystemToOthers(target, translatedMsg, sysOthers.Item1);
            // OpenDAoC Team channel
            else if (table.TryGetValue(eMsg.Team, out var team) && target.Client.Account.PrivLevel > (int)ePrivLevel.Player)
	            target.Client.Out.SendMessage(translatedMsg, team.Item1, team.Item2);
            // All other eMsg types
            else if (table.TryGetValue(type, out var result))
	            target.Client.Out.SendMessage(translatedMsg, result.Item1, result.Item2);
        }

		/// <summary>
        /// Used to send translated messages
        /// </summary>
        /// <param name="type">Determines the eChatType and eChatLoc to use for the message.</param>
        /// <param name="target">The target client receiving the message (e.g., "client")</param>
        /// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
        /// <param name="args">Any argument values to include in the message, such as "client.Player.Name" (if no args, then use "null")</param>
        /// <note>Please paste the translation ID's associated message (English) into a comment above this method.</note>
        /// <example>ChatUtil.SendTypeMessage(eMsg.Command, client, "AdminCommands.Account.Description", null);</example>
        /// <comment>To perform message changes, reference the '.txt' files located in 'GameServer > language > EN'. Please remember, if you change the translation ID value, check all other language folders (e.g., DE, FR, ES, etc.) to ensure all translated strings correctly reflect the new ID.</comment>
        public static void SendTypeMessage(eMsg type, GameClient target, string translationID, params object[] args)
        {
            // See example above for formatting
            var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

            // Announcement messages sent using '/announce'
            if (table.TryGetValue(eMsg.Announce, out var announcement))
            {
	            target.Out.SendMessage(translatedMsg, announcement.Item1, announcement.Item2);
	            target.Out.SendMessage(translatedMsg, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            }
            // Server-wide alerts, send twice for good measure
            else if (table.TryGetValue(eMsg.Server, out var server))
            {
	            target.Out.SendMessage(translatedMsg, server.Item1, server.Item2);
	            target.Out.SendMessage(translatedMsg, server.Item1, server.Item2);
            }
            // Admin debugging messages when troubleshooting behaviors/commands/activities in-game
            else if (table.TryGetValue(eMsg.Debug, out var debug) && target.Account.PrivLevel > (int)ePrivLevel.GM)
	            target.Out.SendMessage(translatedMsg, debug.Item1, debug.Item2);
            // Emote-formatted messages sent to all other players, excluding the originating player
            else if (table.TryGetValue(eMsg.EmoteSysOthers, out var emote))
	            Message.SystemToOthers(target.Player, translatedMsg, emote.Item1);
            // Player death message sent to other nearby players
            else if (table.TryGetValue(eMsg.PlayerDiedOthers, out var diedOthers))
	            Message.SystemToOthers2(target.Player, diedOthers.Item1, translationID, args);
            // System messages sent to a general AoE space
            else if (table.TryGetValue(eMsg.SysArea, out var sysArea))
	            Message.SystemToArea(target.Player, translatedMsg, sysArea.Item1, target.Player);
            // System messages sent to all nearby players, except the target
            else if (table.TryGetValue(eMsg.SysOthers, out var sysOthers))
	            Message.SystemToOthers(target.Player, translatedMsg, sysOthers.Item1);
            // OpenDAoC Team channel
            else if (table.TryGetValue(eMsg.Team, out var team) && target.Account.PrivLevel > (int)ePrivLevel.Player)
	            target.Out.SendMessage(translatedMsg, team.Item1, team.Item2);
            // All other eMsg types
            else if (table.TryGetValue(type, out var result))
	            target.Out.SendMessage(translatedMsg, result.Item1, result.Item2);
        }

		/// <summary>
        /// Used to send various messages
        /// </summary>
        /// <param name="type">Determines the eChatType and eChatLoc to use for the message. Options include: "advise", "alliance", "battlegroup", "bgLeader", "broadcast", "centerSys", "chat", "command", "cmdDesc", "cmdHeader","cmdSyntax", "cmdUsage", "damageAdd", "damaged", "debug", "dialog", "emote", "error", "expires", "group", "guild", "help", "important", "killedByAlb", "killedByHib", "killedByMid", "lfg", "loot", "merchant", "missed", "officer", "othersCombat", "playerDied", "pulse", "resisted", "say", "screenCenter", "send", "skill", "spell", "staff", "sysArea", "sysOthers", "system", "team", "trade", "yell", "youDied", "youHit", "youWereHit".</param>
        /// <param name="target">The target client receiving the message (e.g., "player")</param>
        /// <param name="message">The string message (e.g., "You died!")</param>
        /// <returns>The identified string message</returns>
        /// <example>ChatUtil.SendTypeMessage("system", client, "This is a message.");</example>
        public static void SendTypeMessage(eMsg type, GamePlayer target, string message)
        {
	        // Announcement messages sent using '/announce'
            if (table.TryGetValue(eMsg.Announce, out var announcement))
            {
	            target.Out.SendMessage(message, announcement.Item1, announcement.Item2);
	            target.Out.SendMessage(message, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            }
            // Server-wide alerts, send twice for good measure
            else if (table.TryGetValue(eMsg.Server, out var server))
            {
	            target.Out.SendMessage(message, server.Item1, server.Item2);
	            target.Out.SendMessage(message, server.Item1, server.Item2);
            }
            // Admin debugging messages when troubleshooting behaviors/commands/activities in-game
            else if (table.TryGetValue(eMsg.Debug, out var debug) && target.Client.Account.PrivLevel > (int)ePrivLevel.GM)
	            target.Client.Out.SendMessage(message, debug.Item1, debug.Item2);
            // Emote-formatted messages sent to all other players, excluding the originating player
            else if (table.TryGetValue(eMsg.EmoteSysOthers, out var emote))
	            Message.SystemToOthers(target, message, emote.Item1);
            // Player death message sent to other nearby players
            else if (table.TryGetValue(eMsg.PlayerDiedOthers, out var diedOthers))
	            Message.SystemToOthers2(target, diedOthers.Item1, message);
            // System messages sent to a general AoE space
            else if (table.TryGetValue(eMsg.SysArea, out var sysArea))
	            Message.SystemToArea(target, message, sysArea.Item1, target);
            // System messages sent to all nearby players, except the target
            else if (table.TryGetValue(eMsg.SysOthers, out var sysOthers))
	            Message.SystemToOthers(target, message, sysOthers.Item1);
            // OpenDAoC Team channel
            else if (table.TryGetValue(eMsg.Team, out var team) && target.Client.Account.PrivLevel > (int)ePrivLevel.Player)
	            target.Client.Out.SendMessage(message, team.Item1, team.Item2);
            // All other eMsg types
            else if (table.TryGetValue(type, out var result))
	            target.Client.Out.SendMessage(message, result.Item1, result.Item2);
        }

        /// <summary>
        /// Used to send various messages
        /// </summary>
        /// <param name="type">Determines the eChatType and eChatLoc to use for the message. Options include: "advise", "alliance", "battlegroup", "bgLeader", "broadcast", "centerSys", "chat", "command", "cmdDesc", "cmdHeader","cmdSyntax", "cmdUsage", "damageAdd", "damaged", "debug", "dialog", "emote", "error", "expires", "group", "guild", "help", "important", "killedByAlb", "killedByHib", "killedByMid", "lfg", "loot", "merchant", "missed", "officer", "othersCombat", "playerDied", "pulse", "resisted", "say", "screenCenter", "send", "skill", "spell", "staff", "sysArea", "sysOthers", "system", "team", "trade", "yell", "youDied", "youHit", "youWereHit".</param>
        /// <param name="target">The target client receiving the message (e.g., "client")</param>
        /// <param name="message">The string message (e.g., "You died!")</param>
        /// <returns>The identified string message</returns>
        /// <example>ChatUtil.SendTypeMessage("system", client, "This is a message.");</example>
        public static void SendTypeMessage(eMsg type, GameClient target, string message)
        {
	        // Announcement messages sent using '/announce'
            if (table.TryGetValue(eMsg.Announce, out var announcement))
            {
	            target.Out.SendMessage(message, announcement.Item1, announcement.Item2);
	            target.Out.SendMessage(message, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            }
            // Server-wide alerts, send twice for good measure
            else if (table.TryGetValue(eMsg.Server, out var server))
            {
	            target.Out.SendMessage(message, server.Item1, server.Item2);
	            target.Out.SendMessage(message, server.Item1, server.Item2);
            }
            // Admin debugging messages when troubleshooting behaviors/commands/activities in-game
            else if (table.TryGetValue(eMsg.Debug, out var debug) && target.Account.PrivLevel > (int)ePrivLevel.GM)
	            target.Out.SendMessage(message, debug.Item1, debug.Item2);
            // Emote-formatted messages sent to all other players, excluding the originating player
            else if (table.TryGetValue(eMsg.EmoteSysOthers, out var emote))
	            Message.SystemToOthers(target.Player, message, emote.Item1);
            // Player death message sent to other nearby players
            else if (table.TryGetValue(eMsg.PlayerDiedOthers, out var diedOthers))
	            Message.SystemToOthers2(target.Player, diedOthers.Item1, message);
            // System messages sent to a general AoE space
            else if (table.TryGetValue(eMsg.SysArea, out var sysArea))
	            Message.SystemToArea(target.Player, message, sysArea.Item1, target.Player);
            // System messages sent to all nearby players, except the target
            else if (table.TryGetValue(eMsg.SysOthers, out var sysOthers))
	            Message.SystemToOthers(target.Player, message, sysOthers.Item1);
            // OpenDAoC Team channel
            else if (table.TryGetValue(eMsg.Team, out var team) && target.Account.PrivLevel > (int)ePrivLevel.Player)
	            target.Out.SendMessage(message, team.Item1, team.Item2);
            // All other eMsg types
            else if (table.TryGetValue(type, out var result))
	            target.Out.SendMessage(message, result.Item1, result.Item2);
        }
		
        /// <summary>
		/// Used to send translated messages contained in a text window
		/// </summary>
		/// <param name="type">Determines the type of UI element to send to the target. Current options include: "text", "timer".</param>
		/// <param name="target">The player triggering/receiving the message (i.e., typically "client").</param>
		/// <param name="title">The string to appear along the top border of the text window.</param>
		/// <param name="args">Additional translation IDs or strings to include in the body ot the text window.</param>
		/// <note>To include empty spaces between paragraphs, input a space between apostrophes (e.g., " ").</note>
		public static void SendWindowMessage(eWindow type, GameClient target, string title, params object[] args)
		{
			switch (type)
			{
				case eWindow.Text:
					var info = new List<string>();
					foreach (string translation in args)
						info.Add(LanguageMgr.GetTranslation(target.Account.Language, translation));
			
					target.Out.SendCustomTextWindow(title, info);
					break;
				case eWindow.Timer:
					var timerTitle = LanguageMgr.GetTranslation(target.Account.Language, title);
					var seconds = Convert.ToInt32(args);
					target.Out.SendTimerWindow(timerTitle, seconds);
					break;
			}
		}

		/// <summary>
		/// Used to send string messages contained in a text window
		/// </summary>
		/// <param name="type">Determines the type of UI element to send to the target. Current options include: "text", "timer".</param>
		/// /// <param name="target">The player triggering/receiving the message (i.e., typically "client").</param>
		/// /// <param name="title">The string to appear along the top border of the text window.</param>
		/// <param name="args">Additional translation IDs or strings to include in the body ot the text window.</param>
		/// <note>To include empty spaces between paragraphs, input a space between apostrophies (e.g., " ").</note>
		public static void SendWindowMessage(eWindow type, GamePlayer target, string title, params string[] args)
		{
			switch (type)
			{
				case eWindow.Text:
					var info = new List<string>();
					foreach (var message in args)
						info.Add(message);
			
					target.Client.Out.SendCustomTextWindow(title, info);
					break;
				case eWindow.Timer:
					var timerTitle = LanguageMgr.GetTranslation(target.Client.Account.Language, title);
					var seconds = Convert.ToInt16(args);
					target.Client.Out.SendTimerWindow(timerTitle, seconds);
					break;
			}
		}
		
		public static void SendSystemMessage(GamePlayer target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		public static void SendSystemMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		public static void SendSystemMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		public static void SendSystemMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		public static void SendMerchantMessage(GamePlayer target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow);
		}

		public static void SendMerchantMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow);
		}

		public static void SendMerchantMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow);
		}

		public static void SendMerchantMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated messages to a player, which displays as a dialog (pop-up) window.
		/// </summary>
		/// <param name="target">The player triggering/receiving the message (i.e., typically "client").</param>
		/// <param name="translationID">The translation string associated with the message (e.g., "Scripts.Blacksmith.Say").</param>
		/// <param name="args">Any arguments to include in the message in place of placeholders like "{0}", or else "null".</param>
		public static void SendDialogMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}

		/// <summary>
		/// Used to send translated messages to a player, which displays as a "/say" message in the chat window.
		/// </summary>
		/// <param name="target">The player triggering/receiving the message (i.e., typically "client").</param>
		/// <param name="translationID">The translation string associated with the message (e.g., "Scripts.Blacksmith.Say").</param>
		/// <param name="args">Any arguments to include in the message in place of placeholders like "{0}", or else "null".</param>
		public static void SendSayMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated messages to a player, which displays as a "/say" message in the chat window.
		/// </summary>
		/// <param name="target">The player triggering/receiving the message (i.e., typically "client").</param>
		/// <param name="translationID">The translation string associated with the message (e.g., "Scripts.Blacksmith.Say").</param>
		/// <param name="args">Any arguments to include in the message in place of placeholders like "{0}", or else "null".</param>
		public static void SendSayMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated messages containing slash command descriptions and related information
		/// </summary>
		/// <param name="target">The player triggering/receiving the message (typically "client")</param>
		/// <param name="translationID">The translation string associated with the message (e.g., "AdminCommands.Account.Usage.Create")</param>
		/// <param name="args">Any arguments to include in the message in place of values like "{0}" (or else use "null")</param>
		public static void SendCommMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send in-line messages containing slash command descriptions and related information
		/// </summary>
		/// <param name="target">The player triggering/receiving the message (typically "client")</param>
		/// <param name="message">The message itself (translation IDs recommended instead)</param>
		public static void SendCommMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated messages containing slash command syntax (e.g., /account accountname)
		/// </summary>
		/// <param name="target">The player triggering/receiving the command type list (typically "client")</param>
		/// <param name="translationID">The translation string associated with the message (e.g., "AdminCommands.Account.Syntax.Create")</param>
		/// <param name="args">Any arguments to include in the message in place of values like "{0}" (or else use "null")</param>
		public static void SendSyntaxMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send in-line messages containing slash command syntax (e.g., '/account accountname')
		/// </summary>
		/// <param name="target">The player triggering/receiving the command type list (typically "client")</param>
		/// <param name="message">The message itself (translation IDs recommended instead)</param>
		public static void SendSyntaxMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated header/separator at head of command list (e.g., ----- '/account' Commands (plvl 3) -----)
		/// </summary>
		/// <param name="target">The player triggering/receiving the command type list (typically "client")</param>
		/// <param name="translationID">The translation string associated with the message (e.g., "AdminCommands.Header.Syntax.Account")</param>
		/// <param name="args">Any arguments to include in the message (typically "null")</param>
		public static void SendHeaderMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Used to send in-line header/separator at head of command list (e.g., ----- '/account' Commands (plvl 3) -----)
		/// </summary>
		/// <param name="target">The player triggering/receiving the command type list (typically "client")</param>
		/// <param name="message">The message itself (translation IDs recommended instead)</param>
		public static void SendHeaderMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		
		public static void SendHelpMessage(GamePlayer target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
		}

		/// <summary>
		/// Used to send translated help/alert messages
		/// </summary>
		/// <param name="target">The client receiving the help/alert message (e.g., "player.Client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "target.Client" (if no args, then use "null")</param>
		public static void SendHelpMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
		}

		public static void SendHelpMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
		}

		/// <summary>
		/// Used to send translated help/alert messages
		/// </summary>
		/// <param name="target">The player receiving the help/alert message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendHelpMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated spell resist messages
		/// </summary>
		/// <param name="target">The client receiving the message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendResistMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated spell resist messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "player.Client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendResistMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated error/alert messages
		/// </summary>
		/// <param name="target">The client receiving the error/alert (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendErrorMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated error/alert messages
		/// </summary>
		/// <param name="target">The player client receiving the error/alert (e.g., "player.Client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendErrorMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}

		public static void SendErrorMessage(GamePlayer target, string message)
		{
			SendErrorMessage(target.Client, message);
		}

		public static void SendErrorMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated '/send' messages
		/// </summary>
		/// <param name="target">The client receiving the message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player.Name" (if no args, then use "null")</param>
		public static void SendSendMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Send, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated '/send' messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "player.Client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendSendMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Send, eChatLoc.CL_ChatWindow);
		}

		/// <summary>
		/// Used to send translated '/send' messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "player.Client")</param>
		/// <param name="message">The message string (e.g., "This is a message.")</param>
		public static void SendSendMessage(GamePlayer target, string message)
		{
			SendSendMessage(target.Client, message);
		}

		/// <summary>
		/// Used to send translated '/send' messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "client")</param>
		/// <param name="message">The message string (e.g., "This is a message.")</param>
		public static void SendSendMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Send, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated '/adv' messages
		/// </summary>
		/// <param name="target">The client receiving the message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player.Name" (if no args, then use "null")</param>
		public static void SendAdviceMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Advise, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated '/adv' messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "player.Client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendAdviceMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Advise, eChatLoc.CL_ChatWindow);
		}

		/// <summary>
		/// Used to send translated '/adv' messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "player.Client")</param>
		/// <param name="message">The message string (e.g., "This is a message.")</param>
		public static void SendAdviceMessage(GamePlayer target, string message)
		{
			SendAdviceMessage(target.Client, message);
		}

		/// <summary>
		/// Used to send translated '/adv' messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "client")</param>
		/// <param name="message">The message string (e.g., "This is a message.")</param>
		public static void SendAdviceMessage(GameClient target, string message)
		{
			target.Out.SendMessage(message, eChatType.CT_Advise, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated staff messages
		/// </summary>
		/// <param name="target">The client receiving the message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player.Name" (if no args, then use "null")</param>
		public static void SendGMMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
		}
		
		/// <summary>
		/// Used to send translated team messages
		/// </summary>
		/// <param name="target">The client receiving the message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player.Name" (if no args, then use "null")</param>
		public static void SendTeamMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated staff messages
		/// </summary>
		/// <param name="target">The player client receiving the message (e.g., "player.Client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendGMMessage(GamePlayer target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
		}

		public static void SendDebugMessage(GamePlayer target, string message)
		{
			SendDebugMessage(target.Client, message);
		}

		public static void SendDebugMessage(GameClient target, string message)
		{
			if (target.Account.PrivLevel > (int)ePrivLevel.Player)
				target.Out.SendMessage(message, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated debug messages
		/// </summary>
		/// <param name="target">The player receiving the debug message (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendDebugMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);
			
			if (target.Account.PrivLevel > (int)ePrivLevel.Player)
				target.Out.SendMessage(translatedMsg, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Used to send translated messages to all clients on a server
		/// </summary>
		/// <param name="target">The player receiving the error/alert (e.g., "client")</param>
		/// <param name="translationID">The translation ID for the message (e.g., "AdminCommands.Command.Err.NoPlayerFound")</param>
		/// <param name="args">Any argument values to include in the message, such as "client.Player" (if no args, then use "null")</param>
		public static void SendServerMessage(GameClient target, string translationID, params object[] args)
		{
			var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

			target.Out.SendMessage(translatedMsg, eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
		}
	}
}