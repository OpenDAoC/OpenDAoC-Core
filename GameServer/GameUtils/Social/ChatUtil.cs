using Core.GS.Enums;
using Core.Language;

namespace Core.GS.GameUtils;

public static class ChatUtil
{
	public static void SendSystemMessage(GamePlayer target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	public static void SendSystemMessage(GamePlayer target, string translationID, params object[] args)
	{
		var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

		target.Out.SendMessage(translatedMsg, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	public static void SendSystemMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	public static void SendSystemMessage(GameClient target, string translationID, params object[] args)
	{
		var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

		target.Out.SendMessage(translatedMsg, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	public static void SendMerchantMessage(GamePlayer target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
	}

	public static void SendMerchantMessage(GamePlayer target, string translationID, params object[] args)
	{
		var translatedMsg = LanguageMgr.GetTranslation(target.Client, translationID, args);

		target.Out.SendMessage(translatedMsg, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
	}

	public static void SendMerchantMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
	}

	public static void SendMerchantMessage(GameClient target, string translationID, params object[] args)
	{
		var translatedMsg = LanguageMgr.GetTranslation(target, translationID, args);

		target.Out.SendMessage(translatedMsg, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
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

		target.Out.SendMessage(translatedMsg, EChatType.CT_System, EChatLoc.CL_PopupWindow);
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

		target.Out.SendMessage(translatedMsg, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
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

		target.Out.SendMessage(translatedMsg, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}
	
	/// <summary>
	/// Used to send in-line messages containing slash command descriptions and related information
	/// </summary>
	/// <param name="target">The player triggering/receiving the message (typically "client")</param>
	/// <param name="message">The message itself (translation IDs recommended instead)</param>
	public static void SendCommMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
	}
	
	/// <summary>
	/// Used to send in-line messages containing slash command syntax (e.g., '/account accountname')
	/// </summary>
	/// <param name="target">The player triggering/receiving the command type list (typically "client")</param>
	/// <param name="message">The message itself (translation IDs recommended instead)</param>
	public static void SendSyntaxMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Used to send in-line header/separator at head of command list (e.g., ----- '/account' Commands (plvl 3) -----)
	/// </summary>
	/// <param name="target">The player triggering/receiving the command type list (typically "client")</param>
	/// <param name="message">The message itself (translation IDs recommended instead)</param>
	public static void SendHeaderMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
	}
	
	public static void SendHelpMessage(GamePlayer target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
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

		target.Out.SendMessage(translatedMsg, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
	}

	public static void SendHelpMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
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

		target.Out.SendMessage(translatedMsg, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
	}

	public static void SendErrorMessage(GamePlayer target, string message)
	{
		SendErrorMessage(target.Client, message);
	}

	public static void SendErrorMessage(GameClient target, string message)
	{
		target.Out.SendMessage(message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Send, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Send, EChatLoc.CL_ChatWindow);
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
		target.Out.SendMessage(message, EChatType.CT_Send, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Advise, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Advise, EChatLoc.CL_ChatWindow);
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
		target.Out.SendMessage(message, EChatType.CT_Advise, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Staff, EChatLoc.CL_ChatWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Help, EChatLoc.CL_SystemWindow);
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
		
		target.Out.SendMessage(translatedMsg, EChatType.CT_Staff, EChatLoc.CL_ChatWindow);
	}

	public static void SendDebugMessage(GamePlayer target, string message)
	{
		SendDebugMessage(target.Client, message);
	}

	public static void SendDebugMessage(GameClient target, string message)
	{
		if (target.Account.PrivLevel > (int)EPrivLevel.Player)
			target.Out.SendMessage(message, EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
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
		
		if (target.Account.PrivLevel > (int)EPrivLevel.Player)
			target.Out.SendMessage(translatedMsg, EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
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

		target.Out.SendMessage(translatedMsg, EChatType.CT_Staff, EChatLoc.CL_ChatWindow);
	}
}