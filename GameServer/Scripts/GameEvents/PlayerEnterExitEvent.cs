using System;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.GameEvents
{
	/// <summary>
	/// Spams everyone online with player enter/exit messages
	/// </summary>
	public class PlayerEnterExitEvent
	{
		/// <summary>
		/// Event handler fired when server is started
		/// </summary>
		[GameServerStartedEvent]
		public static void OnServerStart(CoreEvent e, object sender, EventArgs arguments)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(PlayerEntered));
			GameEventMgr.AddHandler(GamePlayerEvent.Quit, new CoreEventHandler(PlayerQuit));
		}

		/// <summary>
		/// Event handler fired when server is stopped
		/// </summary>
		[GameServerStoppedEvent]
		public static void OnServerStop(CoreEvent e, object sender, EventArgs arguments)
		{
			GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(PlayerEntered));
			GameEventMgr.RemoveHandler(GamePlayerEvent.Quit, new CoreEventHandler(PlayerQuit));
		}

		/// <summary>
		/// Event handler fired when players enters the game
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private static void PlayerEntered(CoreEvent e, object sender, EventArgs arguments)
		{
			if (ServerProperties.ServerProperties.SHOW_LOGINS == false)
				return;

			GamePlayer player = sender as GamePlayer;
			if (player == null) return;
			if (player.IsAnonymous) return;

			foreach (GameClient pclient in WorldMgr.GetAllPlayingClients())
			{
				if (player.Client == pclient)
					continue;

				string message = LanguageMgr.GetTranslation(pclient, "Scripts.Events.PlayerEnterExit.Entered", player.Name);

				if (player.Client.Account.PrivLevel > 1)
				{
					message = LanguageMgr.GetTranslation(pclient, "Scripts.Events.PlayerEnterExit.Staff", message);
				}
				else
				{
					string realm = "";
					if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_Normal)
					{
						realm = "[" + GlobalConstants.RealmToName(player.Realm) + "] ";
					}
					message = realm + message;
				}

				EChatType chatType = EChatType.CT_System;

				if (Enum.IsDefined(typeof(EChatType), ServerProperties.ServerProperties.SHOW_LOGINS_CHANNEL))
					chatType = (EChatType)ServerProperties.ServerProperties.SHOW_LOGINS_CHANNEL;

				pclient.Out.SendMessage(message, chatType, EChatLoc.CL_SystemWindow);
			}
		}

		/// <summary>
		/// Event handler fired when player leaves the game
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private static void PlayerQuit(CoreEvent e, object sender, EventArgs arguments)
		{
			if (ServerProperties.ServerProperties.SHOW_LOGINS == false)
				return;

			GamePlayer player = sender as GamePlayer;
			if (player == null) return;
			if (player.IsAnonymous) return;

			foreach (GameClient pclient in WorldMgr.GetAllPlayingClients())
			{
				if (player.Client == pclient)
					continue;

				string message = LanguageMgr.GetTranslation(pclient, "Scripts.Events.PlayerEnterExit.Left", player.Name);

				if (player.Client.Account.PrivLevel > 1)
				{
					message = LanguageMgr.GetTranslation(pclient, "Scripts.Events.PlayerEnterExit.Staff", message);
				}
				else
				{
					string realm = "";
					if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_Normal)
					{
						realm = "[" + GlobalConstants.RealmToName(player.Realm) + "] ";
					}
					message = realm + message;
				}

				EChatType chatType = EChatType.CT_System;

				if (Enum.IsDefined(typeof(EChatType), ServerProperties.ServerProperties.SHOW_LOGINS_CHANNEL))
					chatType = (EChatType)ServerProperties.ServerProperties.SHOW_LOGINS_CHANNEL;

				pclient.Out.SendMessage(message, chatType, EChatLoc.CL_SystemWindow);
			}
		}
	}
}