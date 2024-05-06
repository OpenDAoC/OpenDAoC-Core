using System;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	/// <summary>
	/// This class represents an inventory item when it is
	/// laying on the floor in the world! It is just a wraper
	/// class around InventoryItem
	/// </summary>
	public class WorldInventoryItem : GameStaticItemTimed
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The InventoryItem that is contained within
		/// </summary>
		private DbInventoryItem m_item;

		/// <summary>
		/// Has this item been removed from the world?
		/// </summary>
		private bool m_isRemoved = false;

        public override LanguageDataObject.eTranslationIdentifier TranslationIdentifier
        {
            get { return LanguageDataObject.eTranslationIdentifier.eItem; }
        }


		/// <summary>
		/// Constructs an empty GameInventoryItem
		/// that will disappear from the world after a certain amount of time
		/// </summary>
		public WorldInventoryItem() : base(ServerProperties.Properties.WORLD_ITEM_DECAY_TIME)
		{
		}

		/// <summary>
		/// Constructs a GameInventoryItem based on an
		/// InventoryItem. Will disappear after WORLD_ITEM_DECAY_TIME if
		/// added to the world
		/// </summary>
		/// <param name="item">the InventoryItem to put into this class</param>
		public WorldInventoryItem(DbInventoryItem item) : this()
		{
			m_item = item;
			m_item.SlotPosition = 0;
			m_item.OwnerID = null;
			m_item.AllowAdd = true;
			this.Level = (byte)item.Level;
			this.Model = (ushort)item.Model;
			this.Emblem = item.Emblem;
			this.Name = item.Name;
		}

		/// <summary>
		/// Creates a new GameInventoryItem based on an
		/// InventoryTemplateID. Will disappear after 3 minutes if
		/// added to the world
		/// </summary>
		/// <param name="item">The InventoryItem to load and create an item from</param>
		/// <returns>Found item or null</returns>
		public static WorldInventoryItem CreateFromTemplate(DbInventoryItem item)
		{
			if (item.Template is DbItemUnique)
				return null;
			
			return CreateFromTemplate(item.Id_nb);
		}

		/// <summary>
		/// Creates a new GameInventoryItem based on an
		/// InventoryTemplateID. Will disappear after 3 minutes if
		/// added to the world
		/// </summary>
		/// <param name="templateID">the templateID to load and create an item</param>
		/// <returns>Found item or null</returns>
		public static WorldInventoryItem CreateFromTemplate(string templateID)
		{
			DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);
			if (template == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Item Creation: Template not found!\n"+Environment.StackTrace);
				return null;
			}

			return CreateFromTemplate(template);
		}

		/// <summary>
		/// Creates a new GII handling an UniqueItem in the InventoryItem attached
		/// </summary>
		/// <param name="templateID"></param>
		/// <returns></returns>
		public static WorldInventoryItem CreateUniqueFromTemplate(string templateID)
		{
			DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);

			if (template == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Item Creation: Template not found!\n" + Environment.StackTrace);
				return null;
			}
			
			return CreateUniqueFromTemplate(template);
		}
		
		/// <summary>
		/// Creates a new GameInventoryItem based on an ItemTemplate. Will disappear
		/// after 3 minutes after being added to the world.
		/// </summary>
		/// <param name="template">The template to load and create an item from.</param>
		/// <returns>Item reference or null.</returns>
		public static WorldInventoryItem CreateFromTemplate(DbItemTemplate template)
		{
			if (template == null)
				return null;

			WorldInventoryItem invItem = new WorldInventoryItem();

			invItem.m_item = GameInventoryItem.Create(template);
			
			invItem.m_item.SlotPosition = 0;
			invItem.m_item.OwnerID = null;

			invItem.Level = (byte)template.Level;
			invItem.Model = (ushort)template.Model;
			invItem.Emblem = template.Emblem;
			invItem.Name = template.Name;

			return invItem;
		}

		public static WorldInventoryItem CreateUniqueFromTemplate(DbItemTemplate template)
		{
			if (template == null)
				return null;

			WorldInventoryItem invItem = new WorldInventoryItem();
			DbItemUnique item = new DbItemUnique(template);

			invItem.m_item = GameInventoryItem.Create(item);
			invItem.m_item.SlotPosition = 0;
			invItem.m_item.OwnerID = null;

			invItem.Level = (byte)template.Level;
			invItem.Model = (ushort)template.Model;
			invItem.Emblem = template.Emblem;
			invItem.Name = template.Name;

			return invItem;
		}

		public override bool RemoveFromWorld()
		{
			if (base.RemoveFromWorld())
			{
				(m_item as IGameInventoryItem)?.OnRemoveFromWorld();
				m_isRemoved = true;
				return true;
			}

			return false;
		}

		#region PickUpTimer
		private ECSGameTimer m_pickup;

		/// <summary>
		/// Starts a new pickuptimer with the given time (in seconds)
		/// </summary>
		/// <param name="time"></param>
		public void StartPickupTimer(int time)
		{
			if (m_pickup != null)
			{
				m_pickup.Stop();
				m_pickup = null;
			}
			m_pickup = new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CallBack), time * 1000);
		}

		private int CallBack(ECSGameTimer timer)
		{
			m_pickup.Stop();
			m_pickup = null;
			return 0;
		}

		public void StopPickupTimer()
		{
			foreach (GamePlayer player in Owners)
			{
				if (player.ObjectState == eObjectState.Active)
				{
					player.Out.SendMessage("You may now pick up " + Name + "!", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
				}
			}
			m_pickup.Stop();
			m_pickup = null;
		}

		public int GetPickupTime
		{
			get
			{
				if (m_pickup == null)
					return 0;
				return m_pickup.TimeUntilElapsed;
			}
		}
		#endregion

		/// <summary>
		/// Gets the InventoryItem contained within this class
		/// </summary>
		public DbInventoryItem Item
		{
			get
			{
				return m_item;
			}
		}
	}
}
