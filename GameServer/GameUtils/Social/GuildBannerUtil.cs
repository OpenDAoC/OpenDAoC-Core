using System;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS
{
    public class GuildBannerUtil
    {
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		EcsGameTimer m_timer;
        GamePlayer m_player;
		GuildBannerItem m_item;
        WorldInventoryItem gameItem;

        public GuildBannerUtil(GamePlayer player)
		{
			m_player = player;
		}

        public GamePlayer Player
        {
            get { return m_player; }
        }

		public GuildBannerItem BannerItem
		{
			get { return m_item; }
		}

        public void Start()
        {
            if (m_player.Group != null)
            {
                if (m_player != null)
                {
					bool groupHasBanner = false;

                    foreach (GamePlayer groupPlayer in m_player.Group.GetPlayersInTheGroup())
                    {
                        if (groupPlayer.GuildBanner != null)
						{
							groupHasBanner = true;
							break;
						}
					}

                    if (groupHasBanner == false)
                    {
						if (m_item == null)
						{
							GuildBannerItem item = new GuildBannerItem(GuildBannerTemplate);

							item.OwnerGuild = m_player.Guild;
							item.SummonPlayer = m_player;
							m_item = item;
						}

						m_player.GuildBanner = this;
						m_player.Stealth(false);
						AddHandlers();

                        if (m_timer != null)
                        {
                            m_timer.Stop();
                            m_timer = null;
                        }

                        m_timer = new EcsGameTimer(m_player, new EcsGameTimer.EcsTimerCallback(TimerTick));
                        m_timer.Start(1);

                    }
                    else
                    {
                        m_player.Out.SendMessage("Someone in your group already has a guild banner active!", EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
                    }
                }
                else
                {
                    if (m_timer != null)
                    {
                        m_timer.Stop();
                        m_timer = null;
                    }
                }
            }
            else if (m_player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
            {
                m_player.Out.SendMessage("You have left the group and your guild banner disappears!", EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
                m_player.GuildBanner = null;
                if (m_timer != null)
                {
                    m_timer.Stop();
                    m_timer = null;
                }
            }
        }

        public void Stop()
        {
			RemoveHandlers();
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer = null;
            }
        }

        private int TimerTick(EcsGameTimer timer)
        {
            foreach (GamePlayer player in m_player.GetPlayersInRadius(1500))
            {
                if (player.Group != null && m_player.Group != null && m_player.Group.IsInTheGroup(player))
                {
                    if (GameServer.ServerRules.IsAllowedToAttack(m_player, player, true) == false)
                    {
                        GuildBannerEffect effect = GuildBannerEffect.CreateEffectOfClass(m_player, player);

                        if (effect != null)
                        {
                            GuildBannerEffect oldEffect = player.EffectList.GetOfType(effect.GetType()) as GuildBannerEffect;
							if (oldEffect == null)
							{
								effect.Start(player);
							}
							else
							{
								oldEffect.Stop();
								effect.Start(player);
							}
                        }
                    }
                }
            }

            return 9000; // Pulsing every 9 seconds with a duration of 9 seconds - Tolakram
        }

        protected virtual void AddHandlers()
        {
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.LeaveGroup, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLoseBanner));
            GameEventMgr.AddHandler(m_player, GamePlayerEvent.StealthStateChanged, new CoreEventHandler(PlayerLoseBanner));
            GameEventMgr.AddHandler(m_player, GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.RegionChanging, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.Dying, new CoreEventHandler(PlayerDied));
        }

		protected virtual void RemoveHandlers()
		{
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.LeaveGroup, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.StealthStateChanged, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.RegionChanging, new CoreEventHandler(PlayerLoseBanner));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Dying, new CoreEventHandler(PlayerDied));
		}

		protected void PlayerLoseBanner(CoreEvent e, object sender, EventArgs args)
        {
			Stop();
			m_player.GuildBanner = null;
			m_player.Guild.SendMessageToGuildMembers(string.Format("{0} has put away the guild banner!", m_player.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			m_player = null;
        }

        protected void PlayerDied(CoreEvent e, object sender, EventArgs args)
        {
            DyingEventArgs arg = args as DyingEventArgs;
            if (arg == null) return;
            GameObject killer = arg.Killer as GameObject;
			GamePlayer playerKiller = null;

			if (killer is GamePlayer)
			{
				playerKiller = killer as GamePlayer;
			}
			else if (killer is GameNpc && (killer as GameNpc).Brain != null && (killer as GameNpc).Brain is IControlledBrain)
			{
				playerKiller = ((killer as GameNpc).Brain as IControlledBrain).Owner as GamePlayer;
			}

			Stop();
			m_player.Guild.SendMessageToGuildMembers(m_player.Name + " has dropped the guild banner!", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

			gameItem = new WorldInventoryItem(m_item);
			Point2D point = m_player.GetPointFromHeading(m_player.Heading, 30);
            gameItem.X = point.X;
            gameItem.Y = point.Y;
            gameItem.Z = m_player.Z;
            gameItem.Heading = m_player.Heading;
            gameItem.CurrentRegionID = m_player.CurrentRegionID;
			gameItem.AddOwner(m_player);

			if (playerKiller != null)
			{
				// Guild banner can be picked up by anyone in the enemy group
				if (playerKiller.Group != null)
				{
					foreach (GamePlayer player in playerKiller.Group.GetPlayersInTheGroup())
					{
						gameItem.AddOwner(player);
					}
				}
				else
				{
					gameItem.AddOwner(playerKiller);
				}
			}

			// Guild banner can be picked up by anyone in the dead players group
			if (m_player.Group != null)
			{
				foreach (GamePlayer player in m_player.Group.GetPlayersInTheGroup())
				{
					gameItem.AddOwner(player);
				}
			}

            gameItem.StartPickupTimer(10);
			m_item.OnLose(m_player);
            gameItem.AddToWorld();
        }

        protected DbItemTemplate m_guildBannerTemplate;
        public DbItemTemplate GuildBannerTemplate
        {
            get
            {
                if (m_guildBannerTemplate == null)
                {
					string guildIDNB = "GuildBanner_" + m_player.Guild.GuildID;

					m_guildBannerTemplate = new DbItemTemplate();
					m_guildBannerTemplate.CanDropAsLoot = false;
					m_guildBannerTemplate.Id_nb = guildIDNB;
					m_guildBannerTemplate.IsDropable = false;
					m_guildBannerTemplate.IsPickable = true;
					m_guildBannerTemplate.IsTradable = false;
					m_guildBannerTemplate.IsIndestructible = true;
					m_guildBannerTemplate.Item_Type = 41;
					m_guildBannerTemplate.Level = 1;
					m_guildBannerTemplate.MaxCharges = 1;
					m_guildBannerTemplate.MaxCount = 1;
					m_guildBannerTemplate.Emblem = m_player.Guild.Emblem;
					switch (m_player.Realm)
					{
						case ERealm.Albion:
							m_guildBannerTemplate.Model = 3223;
							break;
						case ERealm.Midgard:
							m_guildBannerTemplate.Model = 3224;
							break;
						case ERealm.Hibernia:
							m_guildBannerTemplate.Model = 3225;
							break;
					}
					m_guildBannerTemplate.Name = m_player.Guild.Name + "'s Banner";
					m_guildBannerTemplate.Object_Type = (int)EObjectType.HouseWallObject;
					m_guildBannerTemplate.Realm = 0;
					m_guildBannerTemplate.Quality = 100;
					m_guildBannerTemplate.ClassType = "DOL.GS.GuildBannerItem";
					m_guildBannerTemplate.PackageID = "GuildBanner";
                }

                return m_guildBannerTemplate;
            }
        }

    }
}



