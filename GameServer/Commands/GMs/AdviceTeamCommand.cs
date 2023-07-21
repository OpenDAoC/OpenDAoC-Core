using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
	[Command(
		"&adviceteam",
		 new [] { "&advt" },
		EPrivLevel.GM,
		// Displays next to the command when '/cmd' is entered
		"Lists all flagged Advisors, sends advisors questions, and sends messages to the Advice channel as Atlas.")]
	public class AdviceTeamCommand : AbstractCommandHandler, ICommandHandler
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
				if ((playerClient.Player.Realm == client.Player.Realm ||
				     playerClient.Account.PrivLevel > 1) && !playerClient.Player.IsIgnoring(client.Player))
				{
					var name = "Atlas";
					// Message: [ADVICE {0}] {1}: {2}
					ChatUtil.SendAdviceMessage(playerClient, "Social.SendAdvice.Msg.Channel", getRealmString(client.Player.Realm), name, msg);
				}

			}
			if (ServerProperties.ServerProperties.DISCORD_ACTIVE) WebhookMessage.LogChatMessage(client.Player, EChatType.CT_Advise, msg);

		}

		private string getRealmString(ERealm Realm)
		{
			switch (Realm)
			{
				case ERealm.Albion: return "ALB";
				case ERealm.Midgard: return "MID";
				case ERealm.Hibernia: return "HIB";
				default: return "NONE";
			}
		}
	}
}