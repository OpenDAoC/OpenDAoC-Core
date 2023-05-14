using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
	[Cmd(
		"&adviceteam",
		 new [] { "&advt" },
		// Message: '/advt <message>' - Sends messages to the Advice channel with the sender labeled as "OpenDAoC".
		"GMCommands.AdviceTeam.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.GM,
		// Message: Allows server staff to send messages to the Advice channel behind a universal name/tag.
		"GMCommands.AdviceTeam.Description",
		// Message: /advt <message>
		"GMCommands.AdviceTeam.Syntax.Message",
		// Message: Sends a message as server staff to the Advice channel.
		"GMCommands.AdviceTeam.Usage.Message"
	)]
	public class AdviceTeamCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{

			if (IsSpammingCommand(client.Player, "adviceteam"))
				return;

			string msg = "";
			if (args.Length >= 2)
			{
				for (int i = 1; i < args.Length; ++i)
				{
					msg += args[i] + " ";
				}
			}
			foreach (GameClient playerClient in WorldMgr.GetAllClients())
			{
				if (playerClient.Player == null) continue;
				
				if (playerClient.Player.Realm == client.Player.Realm && playerClient.Account.PrivLevel > 1)
				{
					var name = "OpenDAoC";

					// Message: [ADVICE {0}] {1}: {2}
					ChatUtil.SendTypeMessage((int)eMsg.Advice, client, "Social.SendAdvice.Msg.Channel", getRealmString(client.Player.Realm), name, msg);
				}
			}
			
			if (Properties.DISCORD_ACTIVE) WebhookMessage.LogChatMessage(client.Player, eChatType.CT_Advise, msg);

		}

		private string getRealmString(eRealm Realm)
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