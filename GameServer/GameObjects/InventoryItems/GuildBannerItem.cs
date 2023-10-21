using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using log4net;

namespace Core.GS
{
	public class GuildBannerItem : GameInventoryItem
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public enum eStatus : byte
		{
			Active = 1,
			Dropped = 2,
			Recovered = 3
		}


		private GuildUtil m_ownerGuild = null;
		private GamePlayer m_summonPlayer = null;
		private eStatus m_status = eStatus.Active;

		public GuildBannerItem()
			: base()
		{
		}

		public GuildBannerItem(DbItemTemplate template)
			: base(template)
		{
		}

		public GuildBannerItem(DbInventoryItem item)
			: base(item)
		{
			OwnerID = item.OwnerID;
			ObjectId = item.ObjectId;
		}

		/// <summary>
		/// What guild owns this banner
		/// </summary>
		public GuildUtil OwnerGuild
		{
			get { return m_ownerGuild; }
			set { m_ownerGuild = value; }
		}

		public GamePlayer SummonPlayer
		{
			get { return m_summonPlayer; }
			set { m_summonPlayer = value; }
		}

		public eStatus Status
		{
			get { return m_status; }
		}


		/// <summary>
		/// Player receives this item (added to players inventory)
		/// </summary>
		/// <param name="player"></param>
		public override void OnReceive(GamePlayer player)
		{
			// for guild banners we don't actually add it to inventory but instead register
			// if it is rescued by a friendly player or taken by the enemy

			player.Inventory.RemoveItem(this);

			int trophyModel = 0;
			ERealm realm = ERealm.None;

			switch (Model)
			{
				case 3223:
					trophyModel = 3359;
					realm = ERealm.Albion;
					break;
				case 3224:
					trophyModel = 3361;
					realm = ERealm.Midgard;
					break;
				case 3225:
					trophyModel = 3360;
					realm = ERealm.Hibernia;
					break;
			}

			// if picked up by an enemy then turn this into a trophy
			if (realm != player.Realm)
			{
				DbItemUnique template = new DbItemUnique(Template);
				template.ClassType = "";
				template.Model = trophyModel;
				template.IsDropable = true;
				template.IsIndestructible = false;

				GameServer.Database.AddObject(template);
				GameInventoryItem trophy = new GameInventoryItem(template);
                player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, trophy);
				OwnerGuild.SendMessageToGuildMembers(player.Name + " of " + GlobalConstants.RealmToName(player.Realm) + " has captured your guild banner!", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				OwnerGuild.GuildBannerLostTime = DateTime.Now;
			}
			else
			{
				m_status = eStatus.Recovered;

				// A friendly player has picked up the banner.
				if (OwnerGuild != null)
				{
					OwnerGuild.SendMessageToGuildMembers(player.Name + " has recovered your guild banner!", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				}

				if (SummonPlayer != null)
				{
					SummonPlayer.GuildBanner = null;
				}
			}
		}

		/// <summary>
		/// Player has dropped, traded, or otherwise lost this item
		/// </summary>
		/// <param name="player"></param>
		public override void OnLose(GamePlayer player)
		{
			if (player.GuildBanner != null)
			{
				player.GuildBanner.Stop();
				m_status = eStatus.Dropped;
			}
		}



		/// <summary>
		/// Drop this item on the ground
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override WorldInventoryItem Drop(GamePlayer player)
		{
			return null;
		}


		public override void OnRemoveFromWorld()
		{
			if (Status == eStatus.Dropped)
			{
				if (SummonPlayer != null)
				{
					SummonPlayer.GuildBanner = null;
					SummonPlayer = null;
				}

				if (OwnerGuild != null)
				{
					// banner was dropped and not picked up, must be re-purchased
					OwnerGuild.GuildBanner = false;
					OwnerGuild.SendMessageToGuildMembers("Your guild banner has been lost!", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					OwnerGuild = null;
				}
			}

			base.OnRemoveFromWorld();
		}


		/// <summary>
		/// Is this a valid item for this player?
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CheckValid(GamePlayer player)
		{
			return false;
		}
	}
}
