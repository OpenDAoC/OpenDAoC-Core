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
*  of message calls used throughout. All messages affected
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
*  language > EN".
*
*  OPTIONAL: After changing a message, paste the new content
*  into the comment above the affected message return(s). This is
*  done for ease of reference. */


using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// ChatUtil for Sending Message to Players
	/// </summary>
	public static class ChatUtil
	{
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
			
			target.Out.SendMessage(translatedMsg, eChatType.CT_Officer, eChatLoc.CL_ChatWindow);
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