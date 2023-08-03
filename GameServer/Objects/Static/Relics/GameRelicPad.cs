using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using JNogueira.Discord.Webhook.Client;
using log4net;

namespace DOL.GS
{
	public class GameRelicPad : GameStaticItem
	{
		const int PAD_AREA_RADIUS = 250;

		PadArea m_area = null;
		GameStaticRelic m_mountedRelic = null;

		#region constructor
		public GameRelicPad()
			: base()
		{
		}
		#endregion

		#region Add/remove from world
		/// <summary>
		/// add the relicpad to world
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			m_area = new PadArea(this);
			CurrentRegion.AddArea(m_area);
			bool success = base.AddToWorld();
			if (success)
			{
				/*
				 * <[RF][BF]Cerian> mid: mjolnerr faste (str)
					<[RF][BF]Cerian> mjollnerr
					<[RF][BF]Cerian> grallarhorn faste (magic)
					<[RF][BF]Cerian> alb: Castle Excalibur (str)
					<[RF][BF]Cerian> Castle Myrddin (magic)
					<[RF][BF]Cerian> Hib: Dun Lamfhota (str), Dun Dagda (magic)
				 */
				//Name = GlobalConstants.RealmToName((DOL.GS.PacketHandler.eRealm)Realm)+ " Relic Pad";
				RelicMgr.AddRelicPad(this);
			}

			return success;
		}

		public override ushort Model
		{
			get
			{
				return 2655;
			}
			set
			{
				base.Model = value;
			}
		}

		public override ERealm Realm
		{
			get
			{
				switch (Emblem)
				{
					case 1:
					case 11:
						return ERealm.Albion;
					case 2:
					case 12:
						return ERealm.Midgard;
					case 3:
					case 13:
						return ERealm.Hibernia;
					default:
						return ERealm.None;
				}
			}
			set
			{
				base.Realm = value;
			}
		}

		public virtual eRelicType PadType
		{
			get
			{
				switch (Emblem)
				{
					case 1:
					case 2:
					case 3:
						return eRelicType.Strength;
					case 11:
					case 12:
					case 13:
						return eRelicType.Magic;
					default:
						return eRelicType.Invalid;

				}
			}
		}

		/// <summary>
		/// removes the relicpad from the world
		/// </summary>
		/// <returns></returns>
		public override bool RemoveFromWorld()
		{
			if (m_area != null)
				CurrentRegion.RemoveArea(m_area);

			return base.RemoveFromWorld();
		}
		#endregion

		/// <summary>
		/// Checks if a GameRelic is mounted at this GameRelicPad
		/// </summary>
		/// <param name="relic"></param>
		/// <returns></returns>
		public bool IsMountedHere(GameStaticRelic relic)
		{
			return m_mountedRelic == relic;
		}
		
		/// <summary>
		/// Method to broadcast RvR messages over Discord
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="realm">The realm</param>
		public static void BroadcastDiscordRelic(string message, ERealm realm, string keepName)
		{
			int color = 0;
			string avatarUrl = "";
			switch (realm)
			{
				case ERealm._FirstPlayerRealm:
					color = 16711680;
					avatarUrl = "https://cdn.discordapp.com/attachments/879754382231613451/977721734948081684/relic_alb.png";
					break;
				case ERealm._LastPlayerRealm:
					color = 32768;
					avatarUrl = "https://cdn.discordapp.com/attachments/879754382231613451/977721735153606686/relic_hib.png";
					break;
				default:
					color = 255;
					avatarUrl = "https://cdn.discordapp.com/attachments/879754382231613451/977721735153606686/relic_hib.png";
					break;
			}
			var client = new DiscordWebhookClient(ServerProperties.ServerProperties.DISCORD_RVR_WEBHOOK_ID);
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

		public void MountRelic(GameStaticRelic relic, bool returning)
		{
			m_mountedRelic = relic;

			if (relic.CurrentCarrier != null && returning == false)
			{
				/* Sending broadcast */
				string message = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "GameRelicPad.MountRelic.Stored", relic.CurrentCarrier.Name, GlobalConstants.RealmToName((ERealm)relic.CurrentCarrier.Realm), relic.Name, Name);
				foreach (GameClient cl in WorldMgr.GetAllPlayingClients())
				{
					if (cl.Player.ObjectState != eObjectState.Active) continue;
                    cl.Out.SendMessage(LanguageMgr.GetTranslation(cl.Account.Language, "GameRelicPad.MountRelic.Captured", GlobalConstants.RealmToName((ERealm)relic.CurrentCarrier.Realm), relic.Name), EChatType.CT_ScreenCenterSmaller, EChatLoc.CL_SystemWindow);
					cl.Out.SendMessage(message + "\n" + message + "\n" + message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				}
				NewsMgr.CreateNews(message, relic.CurrentCarrier.Realm, eNewsType.RvRGlobal, false);
				
				if (ServerProperties.ServerProperties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperties.ServerProperties.DISCORD_RVR_WEBHOOK_ID)))
				{
					BroadcastDiscordRelic(message, relic.CurrentCarrier.Realm, relic.Name);
				}

				/* Increasing of CapturedRelics */
				//select targets to increase CapturedRelics
				//TODO increase stats
				
				BattleGroup relicBG = (BattleGroup)relic.CurrentCarrier?.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);
				List<GamePlayer> targets = new List<GamePlayer>();

				if (relicBG != null)
				{
					lock (relicBG.Members)
					{
						foreach (GamePlayer bgPlayer in relicBG.Members.Keys)
						{
							if (bgPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
							{
								targets.Add(bgPlayer);
							}
						}
					}
				}
				else 
				if (relic.CurrentCarrier.Group != null)
				{
					foreach (GamePlayer p in relic.CurrentCarrier.Group.GetPlayersInTheGroup())
					{
						targets.Add(p);
					}
				}
				else
				{
					targets.Add(relic.CurrentCarrier);
				}

				foreach (GamePlayer target in targets)
				{
					target.CapturedRelics++;
					target.RaiseRealmLoyaltyFloor(2);
					target.Achieve(AchievementUtil.AchievementNames.Relic_Captures);
				}

				relic.LastCaptureDate = DateTime.Now;

				Notify(RelicPadEvent.RelicMounted, this, new RelicPadEventArgs(relic.CurrentCarrier, relic));
			}
			else
			{
				// relic returned to pad, probably because it was dropped on ground and timer expired.
				string message = string.Format("The {0} has been returned to {1}.", relic.Name, Name);
				foreach (GameClient cl in WorldMgr.GetAllPlayingClients())
				{
					if (cl.Player.ObjectState != eObjectState.Active) continue;
					cl.Out.SendMessage(message + "\n" + message + "\n" + message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				}

			}
		}

		public void RemoveRelic(GameStaticRelic relic)
		{
			m_mountedRelic = null;

			if (relic.CurrentCarrier != null)
			{
				string message = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "GameRelicPad.RemoveRelic.Removed", relic.CurrentCarrier.Name, GlobalConstants.RealmToName((ERealm)relic.CurrentCarrier.Realm), relic.Name, Name);
				foreach (GameClient cl in WorldMgr.GetAllPlayingClients())
				{
					if (cl.Player.ObjectState != eObjectState.Active) continue;
					cl.Out.SendMessage(message + "\n" + message + "\n" + message, EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				}
				NewsMgr.CreateNews(message, relic.CurrentCarrier.Realm, eNewsType.RvRGlobal, false);
				
				if (ServerProperties.ServerProperties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(ServerProperties.ServerProperties.DISCORD_RVR_WEBHOOK_ID)))
				{
					BroadcastDiscordRelic(message, relic.CurrentCarrier.Realm, relic.Name);
				}

				Notify(RelicPadEvent.RelicStolen, this, new RelicPadEventArgs(relic.CurrentCarrier, relic));
			}
		}
		
		public int GetEnemiesOnPad()
		{
			var players = GetPlayersInRadius(500);
			var enemyNearby = 0;
				
			foreach (GamePlayer p in players)
			{
				if (p.Realm == Realm) continue;
				enemyNearby++;
			}
			return enemyNearby;
		}

		public void RemoveRelic()
		{
			m_mountedRelic = null;
		}

		public GameStaticRelic MountedRelic
		{
			get { return m_mountedRelic; }
		}

		/// <summary>
		/// Area around the pit that checks if a player brings a GameRelic
		/// </summary>
		public class PadArea : Area.Circle
		{
			private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

			GameRelicPad m_parent;

			public PadArea(GameRelicPad parentPad)
				: base("", parentPad.X, parentPad.Y, parentPad.Z, PAD_AREA_RADIUS)
			{
				m_parent = parentPad;
			}

			public override void OnPlayerEnter(GamePlayer player)
			{
				GameStaticRelic relicOnPlayer = player.TempProperties.getProperty<object>(GameStaticRelic.PLAYER_CARRY_RELIC_WEAK, null) as GameStaticRelic;
				if (relicOnPlayer == null)
				{
					return;
				}

				if (relicOnPlayer.RelicType != m_parent.PadType
				    // || m_parent.MountedRelic != null
				    )
				{
                    player.Client.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameRelicPad.OnPlayerEnter.EmptyRelicPad"), relicOnPlayer.RelicType), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					log.DebugFormat("Player {0} needs to find an empty {1} relic pad in order to place {2}.", player.Name, relicOnPlayer.RelicType, relicOnPlayer.Name);
					return;
				}

				if (player.Realm == m_parent.Realm)
				{
					log.DebugFormat("Player {0} captured relic {1}.", player.Name, relicOnPlayer.Name);
					relicOnPlayer.RelicPadTakesOver(m_parent, false);
				}
				else
				{
					log.DebugFormat("Player realm {0} wrong realm on attempt to capture relic {1} of realm {2} on pad of realm {3}.",
					                GlobalConstants.RealmToName(player.Realm),
					                relicOnPlayer.Name,
					                GlobalConstants.RealmToName(relicOnPlayer.Realm),
					                GlobalConstants.RealmToName(m_parent.Realm));
				}
			}
		}

	}
}