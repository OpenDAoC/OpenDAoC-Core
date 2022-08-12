using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using JNogueira.Discord.Webhook.Client;
using log4net;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// The type of interaction we check for to handle lord permission checks
	/// </summary>
	public enum eInteractType
	{ 
		/// <summary>
		/// Claim the Area
		/// </summary>
		Claim,
		/// <summary>
		/// Release the Area
		/// </summary>
		Release,
		/// <summary>
		/// Change the level of the Area
		/// </summary>
		ChangeLevel,
	}

	/// <summary>
	/// Class to manage all the dealings with Players
	/// </summary>
	public class PlayerMgr
	{
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Sends a message to all players to notify them of the keep capture
		/// </summary>
		/// <param name="keep">The keep object</param>
		public static void BroadcastCapture(AbstractGameKeep keep)
		{
			string message = "";
			if (keep.Realm != eRealm.None)
			{
				message = string.Format(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "PlayerManager.BroadcastCapture.Captured", GlobalConstants.RealmToName((eRealm)keep.Realm), keep.Name));
			}
			else
			{
                message = string.Format(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "PlayerManager.BroadcastCapture.CapturedR0", GlobalConstants.RealmToName((eRealm)keep.Realm), keep.Name));
			}

			/*
			switch (GameServer.Instance.Configuration.ServerType)
			{
				case eGameServerType.GST_Normal:
					{
						message = string.Format("The forces of {0} have captured {1}!", GlobalConstants.RealmToName((eRealm)keep.Realm), keep.Name);
						break;
					}
				case eGameServerType.GST_PvP:
					{
						string defeatersStr = "";
						message = string.Format("The forces of {0} have defeated the defenders of {1}!", defeatersStr, keep.Name);
						break;
					}
			}*/

			BroadcastKeepTakeMessage(message, keep.Realm);
			NewsMgr.CreateNews(message, keep.Realm, eNewsType.RvRGlobal, false);

			if (ServerProperties.Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID)))
			{
				BroadcastDiscordRvR(message, keep.Realm, keep.Name);
			}
			
		}

		/// <summary>
		/// Sends a message to all players to notify them of the raize
		/// </summary>
		/// <param name="keep">The keep object</param>
		/// <param name="realm">The raizing realm</param>
		public static void BroadcastRaize(AbstractGameKeep keep, eRealm realm)
		{
			string message = string.Format(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "PlayerManager.BroadcastRaize.Razed", keep.Name, GlobalConstants.RealmToName(realm)));
			BroadcastMessage(message, eRealm.None);
			NewsMgr.CreateNews(message, keep.Realm, eNewsType.RvRGlobal, false);
		}

		/// <summary>
		/// Sends a message to all players of a realm, to notify them of a claim
		/// </summary>
		/// <param name="keep">The keep object</param>
		public static void BroadcastClaim(AbstractGameKeep keep)
		{

			string claimMessage = string.Format(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE,
				"PlayerManager.BroadcastClaim.Claimed", keep.Guild.Name, keep.Name));
			
			BroadcastMessage(claimMessage, (eRealm)keep.Realm);
			
			// if (ServerProperties.Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_WEBHOOK_ID)))
			// {
			// 	BroadcastDiscordRvR(claimMessage, keep.Realm, keep.Name);
			// }
		}

		/// <summary>
		/// Sends a message to all players of a realm, to notify them of a release
		/// </summary>
		/// <param name="keep">The keep object</param>
		public static void BroadcastRelease(AbstractGameKeep keep)
		{
			string lostClaimMessage = string.Format(LanguageMgr.GetTranslation(
				ServerProperties.Properties.SERV_LANGUAGE, "PlayerManager.BroadcastRelease.LostControl",
				keep.Guild.Name, keep.Name));
			
			BroadcastMessage(lostClaimMessage, (eRealm)keep.Realm);
			
			// if (ServerProperties.Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_WEBHOOK_ID)))
			// {
			// 	BroadcastDiscordRvR(lostClaimMessage, keep.Guild.Realm, keep.Name);
			// }
			
		}

		/// <summary>
		/// Method to broadcast messages, if eRealm.None all can see,
		/// else only the right realm can see
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="realm">The realm</param>
		public static void BroadcastMessage(string message, eRealm realm)
		{
			foreach (GameClient client in WorldMgr.GetAllClients())
			{
				if (client.Player == null)
					continue;
				if ((client.Account.PrivLevel != 1 || realm == eRealm.None) || client.Player.Realm == realm)
				{
					client.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				}
			}
		}

		/// <summary>
		/// Method to broadcast keep take messages and sounds
		/// else only the right realm can see
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="capturingrealm">The realm that captured the keep</param>
		public static void BroadcastKeepTakeMessage(string message, eRealm capturingrealm)
		{
			foreach (GameClient client in WorldMgr.GetAllClients())
			{
				if (client.Player == null)
					continue;
				
				client.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(message, eChatType.CT_ScreenCenter,  eChatLoc.CL_SystemWindow);
				
				switch (capturingrealm)
				{
					case eRealm.Albion:
						client.Out.SendSoundEffect(220, 0, 0, 0, 0, 0);
						break;
					case eRealm.Midgard:
						client.Out.SendSoundEffect(218, 0, 0, 0, 0, 0);
						break;
					case eRealm.Hibernia:
						client.Out.SendSoundEffect(219, 0, 0, 0, 0, 0);
						break;
				}

			}
		}
		
		/// <summary>
		/// Method to broadcast RvR messages over Discord
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="realm">The realm</param>
		public static void BroadcastDiscordRvR(string message, eRealm realm, string keepName)
		{
			int color = 0;
			string avatarUrl = "";
			switch (realm)
			{
				case eRealm._FirstPlayerRealm:
					color = 16711680;
					avatarUrl = "https://cdn.discordapp.com/attachments/919610633656369214/928728399822860369/keep_alb.png";
					break;
				case eRealm._LastPlayerRealm:
					color = 32768;
					avatarUrl = "https://cdn.discordapp.com/attachments/919610633656369214/928728400116478073/keep_hib.png";
					break;
				default:
					color = 255;
					avatarUrl = "https://cdn.discordapp.com/attachments/919610633656369214/928728400523296768/keep_mid.png";
					break;
			}
			var client = new DiscordWebhookClient(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID);

			// Create your DiscordMessage with all parameters of your message.
			var discordMessage = new DiscordMessage(
				"",
				username: "Atlas RvR",
				avatarUrl: avatarUrl,
				tts: false,
				embeds: new[]
				{
					new DiscordMessageEmbed(
						author: new DiscordMessageEmbedAuthor(keepName),
						color: color,
						description: message
					)
				}
			);
			
			client.SendToDiscord(discordMessage);
		}

		/// <summary>
		/// Method to popup message on area enter
		/// </summary>
		/// <param name="player">The target of the message</param>
		/// <param name="message">The message</param>
		public static void PopupAreaEnter(GamePlayer player, string message)
		{
			/*
			 * Blood of the Realm has claimed this outpost.
			 */
			player.Out.SendMessage(message + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Method to tell us if a player can interact with the lord to do certain tasks
		/// </summary>
		/// <param name="player">The player object</param>
		/// <param name="keep">The area object</param>
		/// <param name="type">The type of interaction</param>
		/// <returns></returns>
		public static bool IsAllowedToInteract(GamePlayer player, AbstractGameKeep keep, eInteractType type)
		{
			if (player.Client.Account.PrivLevel > 1)
				return true;
			if (player.Realm != keep.Realm)
				return false;
			if (player.Guild == null)
				return false;

			if (keep.InCombat)
			{
				log.DebugFormat("KEEPWARNING: {0} attempted to {1} {2} while in combat.", player.Name, type, keep.Name);
				return false;
			}

			switch (type)
			{
				case eInteractType.Claim:
					{
						if (keep.Guild != null)
							return false;
						foreach (AbstractGameKeep k in GameServer.KeepManager.GetAllKeeps())
						{
							if (k.Guild == player.Guild)
								return false;
						}
						if (player.Group == null)
							return false;
						if (player.Group.Leader != player)
							return false;
						if (player.Group.MemberCount < ServerProperties.Properties.CLAIM_NUM)
							return false;
						if (!player.GuildRank.Claim)
							return false;
						break;
					}
				case eInteractType.Release:
					{
						if (keep.Guild == null)
							return false;
						if (keep.Guild != player.Guild)
							return false;
						if (!player.GuildRank.Claim)
							return false;
						break;
					}
				case eInteractType.ChangeLevel:
					{
						if (keep.Guild == null)
							return false;
						if (keep.Guild != player.Guild)
							return false;
						if (!player.GuildRank.Claim)
							return false;
						break;
					}
			}
			return true;
		}

		/// <summary>
		/// Method to update stats for all players who helped kill lord
		/// </summary>
		/// <param name="lord">The lord object</param>
		public static void UpdateStats(GuardLord lord)
		{
			lock (lord.XPGainers.SyncRoot)
			{
				foreach (System.Collections.DictionaryEntry de in lord.XPGainers)
				{
					GameObject obj = (GameObject)de.Key;
					if (obj is GamePlayer)
					{
						GamePlayer player = obj as GamePlayer;
						if (lord.Component.Keep != null && lord.Component.Keep is GameKeep)
						{
							player.CapturedKeeps++;
							player.Achieve(AchievementUtils.AchievementNames.Keeps_Taken);
						}
							
						else player.CapturedTowers++;
						
						if(player.CapturedKeeps % 25 == 0)
							player.RaiseRealmLoyaltyFloor(1);
					}
				}
			}
		}
	}
}
