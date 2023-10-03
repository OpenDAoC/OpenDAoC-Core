using System;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.GameEvents
{
	/// <summary>
	/// Spams everyone online with player enter/exit messages
	/// </summary>
	public class PlayerEnterExit
	{
		/// <summary>
		/// Event handler fired when server is started
		/// </summary>
		[GameServerStartedEvent]
		public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
			GameEventMgr.AddHandler(GamePlayerEvent.Quit, new DOLEventHandler(PlayerQuit));
		}

		/// <summary>
		/// Event handler fired when server is stopped
		/// </summary>
		[GameServerStoppedEvent]
		public static void OnServerStop(DOLEvent e, object sender, EventArgs arguments)
		{
			GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
			GameEventMgr.RemoveHandler(GamePlayerEvent.Quit, new DOLEventHandler(PlayerQuit));
		}

		/// <summary>
		/// Event handler fired when players enters the game
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private static void PlayerEntered(DOLEvent e, object sender, EventArgs arguments)
		{
			if (!ServerProperties.Properties.SHOW_LOGINS || sender is not GamePlayer player || player.IsAnonymous)
				return;

			foreach (GamePlayer otherPlayer in ClientService.GetPlayers())
			{
				if (player == otherPlayer)
					continue;

				string message = LanguageMgr.GetTranslation(otherPlayer.Client, "Scripts.Events.PlayerEnterExit.Entered", player.Name);

				if (player.Client.Account.PrivLevel > 1)
				{
					message = LanguageMgr.GetTranslation(otherPlayer.Client, "Scripts.Events.PlayerEnterExit.Staff", message);
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

				eChatType chatType = eChatType.CT_System;

				if (Enum.IsDefined(typeof(eChatType), ServerProperties.Properties.SHOW_LOGINS_CHANNEL))
					chatType = (eChatType)ServerProperties.Properties.SHOW_LOGINS_CHANNEL;

				otherPlayer.Out.SendMessage(message, chatType, eChatLoc.CL_SystemWindow);
			}
		}

		/// <summary>
		/// Event handler fired when player leaves the game
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private static void PlayerQuit(DOLEvent e, object sender, EventArgs arguments)
		{
			if (ServerProperties.Properties.SHOW_LOGINS == false)
				return;

			GamePlayer player = sender as GamePlayer;
			if (player == null) return;
			if (player.IsAnonymous) return;

			foreach (GamePlayer otherPlayer in ClientService.GetPlayers())
			{
				if (player == otherPlayer)
					continue;

				string message = LanguageMgr.GetTranslation(otherPlayer.Client, "Scripts.Events.PlayerEnterExit.Left", player.Name);

				if (player.Client.Account.PrivLevel > 1)
				{
					message = LanguageMgr.GetTranslation(otherPlayer.Client, "Scripts.Events.PlayerEnterExit.Staff", message);
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

				eChatType chatType = eChatType.CT_System;

				if (Enum.IsDefined(typeof(eChatType), ServerProperties.Properties.SHOW_LOGINS_CHANNEL))
					chatType = (eChatType)ServerProperties.Properties.SHOW_LOGINS_CHANNEL;

				otherPlayer.Out.SendMessage(message, chatType, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
