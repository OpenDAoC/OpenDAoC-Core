using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Server;
using JNogueira.Discord.Webhook.Client;
using log4net;

namespace Core.GS.Keeps;

/// <summary>
/// Class to manage all the dealings with Players
/// </summary>
public class KeepPlayerMgr
{
	private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Sends a message to all players to notify them of the keep capture
	/// </summary>
	/// <param name="keep">The keep object</param>
	public static void BroadcastCapture(AGameKeep keep)
	{
		string message = "";
		if (keep.Realm != ERealm.None)
		{
			message = string.Format(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "PlayerManager.BroadcastCapture.Captured", GlobalConstants.RealmToName((ERealm)keep.Realm), keep.Name));
		}
		else
		{
            message = string.Format(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "PlayerManager.BroadcastCapture.CapturedR0", GlobalConstants.RealmToName((ERealm)keep.Realm), keep.Name));
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
		NewsMgr.CreateNews(message, keep.Realm, ENewsType.RvRGlobal, false);

		if (ServerProperty.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperty.DISCORD_RVR_WEBHOOK_ID)))
		{
			BroadcastDiscordRvR(message, keep.Realm, keep.Name);
		}
		
	}

	/// <summary>
	/// Sends a message to all players to notify them of the raize
	/// </summary>
	/// <param name="keep">The keep object</param>
	/// <param name="realm">The raizing realm</param>
	public static void BroadcastRaize(AGameKeep keep, ERealm realm)
	{
		string message = string.Format(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "PlayerManager.BroadcastRaize.Razed", keep.Name, GlobalConstants.RealmToName(realm)));
		BroadcastMessage(message, ERealm.None);
		NewsMgr.CreateNews(message, keep.Realm, ENewsType.RvRGlobal, false);
	}

	/// <summary>
	/// Sends a message to all players of a realm, to notify them of a claim
	/// </summary>
	/// <param name="keep">The keep object</param>
	public static void BroadcastClaim(AGameKeep keep)
	{

		string claimMessage = string.Format(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE,
			"PlayerManager.BroadcastClaim.Claimed", keep.Guild.Name, keep.Name));
		
		BroadcastMessage(claimMessage, (ERealm)keep.Realm);
		
		// if (ServerProperties.Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_WEBHOOK_ID)))
		// {
		// 	BroadcastDiscordRvR(claimMessage, keep.Realm, keep.Name);
		// }
	}

	/// <summary>
	/// Sends a message to all players of a realm, to notify them of a release
	/// </summary>
	/// <param name="keep">The keep object</param>
	public static void BroadcastRelease(AGameKeep keep)
	{
		string lostClaimMessage = string.Format(LanguageMgr.GetTranslation(
			ServerProperty.SERV_LANGUAGE, "PlayerManager.BroadcastRelease.LostControl",
			keep.Guild.Name, keep.Name));
		
		BroadcastMessage(lostClaimMessage, (ERealm)keep.Realm);
		
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
	public static void BroadcastMessage(string message, ERealm realm)
	{
		foreach (GamePlayer player in ClientService.GetPlayersOfRealm(realm))
			player.Out.SendMessage(message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Method to broadcast keep take messages and sounds
	/// else only the right realm can see
	/// </summary>
	/// <param name="message">The message</param>
	/// <param name="capturingrealm">The realm that captured the keep</param>
	public static void BroadcastKeepTakeMessage(string message, ERealm capturingrealm)
	{
		foreach (GamePlayer player in ClientService.GetPlayers())
		{
			player.Out.SendMessage(message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			player.Out.SendMessage(message, EChatType.CT_ScreenCenter,  EChatLoc.CL_SystemWindow);
			
			switch (capturingrealm)
			{
				case ERealm.Albion:
					player.Out.SendSoundEffect(220, 0, 0, 0, 0, 0);
					break;
				case ERealm.Midgard:
					player.Out.SendSoundEffect(218, 0, 0, 0, 0, 0);
					break;
				case ERealm.Hibernia:
					player.Out.SendSoundEffect(219, 0, 0, 0, 0, 0);
					break;
			}
		}
	}

	/// <summary>
	/// Method to broadcast RvR messages over Discord
	/// </summary>
	/// <param name="message">The message</param>
	/// <param name="realm">The realm</param>
	public static void BroadcastDiscordRvR(string message, ERealm realm, string keepName)
	{
		int color = 0;
		string avatarUrl = "";
		switch (realm)
		{
			case ERealm._FirstPlayerRealm:
				color = 16711680;
				avatarUrl = "";
				break;
			case ERealm._LastPlayerRealm:
				color = 32768;
				avatarUrl = "";
				break;
			default:
				color = 255;
				avatarUrl = "";
				break;
		}
		var client = new DiscordWebhookClient(ServerProperty.DISCORD_RVR_WEBHOOK_ID);

		// Create your DiscordMessage with all parameters of your message.
		var discordMessage = new DiscordMessage(
			"",
			username: "RvR",
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
		player.Out.SendMessage(message + ".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		player.Out.SendMessage(message, EChatType.CT_ScreenCenterSmaller, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Method to tell us if a player can interact with the lord to do certain tasks
	/// </summary>
	/// <param name="player">The player object</param>
	/// <param name="keep">The area object</param>
	/// <param name="type">The type of interaction</param>
	/// <returns></returns>
	public static bool IsAllowedToInteract(GamePlayer player, AGameKeep keep, EKeepInteractType type)
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
			case EKeepInteractType.Claim:
				{
					if (keep.Guild != null)
						return false;
					foreach (AGameKeep k in GameServer.KeepManager.GetAllKeeps())
					{
						if (k.Guild == player.Guild)
							return false;
					}
					if (player.Group == null)
						return false;
					if (player.Group.Leader != player)
						return false;
					if (player.Group.MemberCount < ServerProperty.CLAIM_NUM)
						return false;
					if (!player.GuildRank.Claim)
						return false;
					break;
				}
			case EKeepInteractType.Release:
				{
					if (keep.Guild == null)
						return false;
					if (keep.Guild != player.Guild)
						return false;
					if (!player.GuildRank.Claim)
						return false;
					break;
				}
			case EKeepInteractType.ChangeLevel:
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
						player.Achieve(AchievementUtil.AchievementName.Keeps_Taken);
					}
						
					else player.CapturedTowers++;
					
					if(player.CapturedKeeps % 25 == 0)
						player.RaiseRealmLoyaltyFloor(1);
				}
			}
		}
	}
}