using System;
using Core.Base.Enums;
using Core.Events;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.GameEvents
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

				EChatType chatType = EChatType.CT_System;

				if (Enum.IsDefined(typeof(EChatType), ServerProperties.Properties.SHOW_LOGINS_CHANNEL))
					chatType = (EChatType)ServerProperties.Properties.SHOW_LOGINS_CHANNEL;

				otherPlayer.Out.SendMessage(message, chatType, EChatLoc.CL_SystemWindow);
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

				EChatType chatType = EChatType.CT_System;

				if (Enum.IsDefined(typeof(EChatType), ServerProperties.Properties.SHOW_LOGINS_CHANNEL))
					chatType = (EChatType)ServerProperties.Properties.SHOW_LOGINS_CHANNEL;

				otherPlayer.Out.SendMessage(message, chatType, EChatLoc.CL_SystemWindow);
			}
		}
	}
}
