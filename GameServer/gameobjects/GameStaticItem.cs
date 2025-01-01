using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// This class represents a static Item in the gameworld
	/// </summary>
	public class GameStaticItem : GameObject , ITranslatableObject
	{
		/// <summary>
		/// The emblem of the Object
		/// </summary>
		protected int m_Emblem;

		/// <summary>
		/// The respawn interval of this world object
		/// </summary>
		protected int m_respawnInterval = 0;

		public int RespawnInterval
		{
			get { return m_respawnInterval; }
			set	{ m_respawnInterval = value; }
		}

		public override eGameObjectType GameObjectType => eGameObjectType.ITEM;

		public GameStaticItem() : base() { }

		#region Name/Model/GetName/GetExamineMessages
		/// <summary>
		/// gets or sets the model of this Item
		/// </summary>
		public override ushort Model
		{
			get { return base.Model; }
			set
			{
				base.Model = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateObjectForPlayers(this);
			}
		}

		/// <summary>
		/// Gets or Sets the current Emblem of the Object
		/// </summary>
		public virtual int Emblem
		{
			get { return m_Emblem; }
			set
			{
				m_Emblem = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateObjectForPlayers(this);
			}
		}

        public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier
        {
            get { return LanguageDataObject.eTranslationIdentifier.eObject; }
        }

        /// <summary>
        /// The translation id
        /// </summary>
        protected string m_translationId = string.Empty;

        /// <summary>
        /// Gets or sets the translation id
        /// </summary>
        public string TranslationId
        {
            get { return m_translationId; }
            set { m_translationId = (value == null ? "" : value); }
        }

		/// <summary>
		/// Gets or sets the name of this item
		/// </summary>
		public override string Name
		{
			get 
			{
				return base.Name; 
			}
			set
			{
				base.Name = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateObjectForPlayers(this);
			}
		}

        /// <summary>
        /// Holds the examine article
        /// </summary>
        private string m_examineArticle = string.Empty;
        /// <summary>
        /// Gets or sets the examine article
        /// </summary>
        public string ExamineArticle
        {
            get { return m_examineArticle; }
            set { m_examineArticle = (value == null ? "" : value); }
        }
		
		private bool m_loadedFromScript = true;
		public bool LoadedFromScript
		{
			get { return m_loadedFromScript; }
			set { m_loadedFromScript = value; }
		}



		/// <summary>
		/// Returns name with article for nouns
		/// </summary>
		/// <param name="article">0=definite, 1=indefinite</param>
		/// <param name="firstLetterUppercase">Forces the first letter of the returned string to be uppercase</param>
		/// <returns>name of this object (includes article if needed)</returns>
		public override string GetName(int article, bool firstLetterUppercase)
		{
			if (Name == string.Empty)
				return string.Empty;

			if(char.IsUpper(Name[0]))
			{
				// proper name

				if (firstLetterUppercase)
					return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStaticItem.GetName.Article1", Name);
				else
					return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStaticItem.GetName.Article2", Name);
			}
			else
			{
				// common noun
				return base.GetName(article, firstLetterUppercase);
			}
		}

		/// <summary>
		/// Adds messages to ArrayList which are sent when object is targeted
		/// </summary>
		/// <param name="player">GamePlayer that is examining this object</param>
		/// <returns>list with string messages</returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list = base.GetExamineMessages(player);
			list.Insert(0, "You select "+ GetName(0, false) +".");
			return list;
		}
		#endregion

		public override void LoadFromDatabase(DataObject obj)
		{
			DbWorldObject item = obj as DbWorldObject;
			base.LoadFromDatabase(obj);
			
			m_loadedFromScript = false;
			CurrentRegionID = item.Region;
            TranslationId = item.TranslationId;
			Name = item.Name;
            ExamineArticle = item.ExamineArticle;
			Model = item.Model;
			Emblem = item.Emblem;
			Realm = (eRealm)item.Realm;
			Heading = item.Heading;
			X = item.X;
			Y = item.Y;
			Z = item.Z;
			RespawnInterval = item.RespawnInterval;
		}

		/// <summary>
		/// Gets or sets the heading of this item
		/// </summary>
		public override ushort Heading
		{
			get { return base.Heading; }
			set
			{
				base.Heading = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateObjectForPlayers(this);
			}
		}

		/// <summary>
		/// Gets or sets the level of this item
		/// </summary>
		public override byte Level
		{
			get { return base.Level; }
			set
			{
				base.Level = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateObjectForPlayers(this);
			}
		}

		/// <summary>
		/// Gets or sets the realm of this item
		/// </summary>
		public override eRealm Realm
		{
			get { return base.Realm; }
			set
			{
				base.Realm = value;
			}
		}

		/// <summary>
		/// Saves this Item in the WorldObject DB
		/// </summary>
		public override void SaveIntoDatabase()
		{
			DbWorldObject obj = null;
			if (InternalID != null)
			{
				obj = (DbWorldObject)GameServer.Database.FindObjectByKey<DbWorldObject>(InternalID);
			}
			if (obj == null)
			{
				if (LoadedFromScript == false)
				{
					obj = new DbWorldObject();
				}
				else
				{
					return;
				}
			}
            obj.TranslationId = TranslationId;
			obj.Name = Name;
            obj.ExamineArticle = ExamineArticle;
			obj.Model = Model;
			obj.Emblem = Emblem;
			obj.Realm = (byte)Realm;
			obj.Heading = Heading;
			obj.Region = CurrentRegionID;
			obj.X = X;
			obj.Y = Y;
			obj.Z = Z;
			obj.ClassType = this.GetType().ToString();
			obj.RespawnInterval = RespawnInterval;

			if (InternalID == null)
			{
				GameServer.Database.AddObject(obj);
				InternalID = obj.ObjectId;
			}
			else
			{
				GameServer.Database.SaveObject(obj);
			}
		}

		public override void Delete()
		{
			Notify(GameObjectEvent.Delete, this);
			RemoveFromWorld(0); // will not respawn
			ObjectState = eObjectState.Deleted;
		}

		/// <summary>
		/// Deletes this item from the WorldObject DB
		/// </summary>
		public override void DeleteFromDatabase()
		{
			if(InternalID != null)
			{
				DbWorldObject obj = (DbWorldObject) GameServer.Database.FindObjectByKey<DbWorldObject>(InternalID);
				if(obj != null)
				  GameServer.Database.DeleteObject(obj);
			}
			InternalID = null;
		}

		/// <summary>
		/// Called to create an item in the world
		/// </summary>
		/// <returns>true when created</returns>
		public override bool AddToWorld()
		{
			if (!base.AddToWorld())
				return false;

			ClientService.CreateObjectForPlayers(this);
			return true;
		}

		/// <summary>
		/// Called to remove the item from the world
		/// </summary>
		/// <returns>true if removed</returns>
		public override bool RemoveFromWorld()
		{
			return RemoveFromWorld(RespawnInterval);
		}

		/// <summary>
		/// Temporarily remove this static item from the world.
		/// Used mainly for quest interaction
		/// </summary>
		/// <param name="respawnSeconds"></param>
		/// <returns></returns>
		public virtual bool RemoveFromWorld(int respawnSeconds)
		{
			if (base.RemoveFromWorld())
			{
				if (respawnSeconds > 0)
					StartRespawn(Math.Max(1, respawnSeconds));

				return true;
			}

			return false;
		}

		/// <summary>
		/// Timer used to respawn this object
		/// </summary>
		protected ECSGameTimer m_respawnTimer = null;

		/// <summary>
		/// The sync object for respawn timer modifications
		/// </summary>
		protected readonly Lock _respawnTimerLock = new();

		/// <summary>
		/// Starts the Respawn Timer
		/// </summary>
		protected virtual void StartRespawn(int respawnSeconds)
		{
			lock (_respawnTimerLock)
			{
				if (m_respawnTimer == null)
				{
					m_respawnTimer = new ECSGameTimer(this);
					m_respawnTimer.Callback = new ECSGameTimer.ECSTimerCallback(RespawnTimerCallback);
					m_respawnTimer.Start(respawnSeconds * 1000);
				}
			}
		}

		/// <summary>
		/// The callback that will respawn this object
		/// </summary>
		/// <param name="respawnTimer">the timer calling this callback</param>
		/// <returns>the new interval</returns>
		protected virtual int RespawnTimerCallback(ECSGameTimer respawnTimer)
		{
			lock (_respawnTimerLock)
			{
				if (m_respawnTimer != null)
				{
					m_respawnTimer.Stop();
					m_respawnTimer = null;
					AddToWorld();
				}
			}

			return 0;
		}

		public HashSet<IGameStaticItemOwner> Owners { get; private set; }

		public void AddOwner(IGameStaticItemOwner owner)
		{
			Owners ??= new();
			Owners.Add(owner);
		}

		public bool IsOwner(GamePlayer player)
		{
			if (Owners == null)
				return false;

			if (Owners.Contains(player) || Owners.Contains(player.Group))
				return true;

			BattleGroup battleGroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
			return battleGroup != null && Owners.Contains(battleGroup);
		}
	}

	public interface IGameStaticItemOwner
	{
		string Name { get; }
		bool TryAutoPickUpMoney(GameMoney money);
		bool TryAutoPickUpItem(WorldInventoryItem item);
		TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money); // Expected to return false only if the object shouldn't try to pick up the item at all.
		TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item); // Expected to return false only if the object shouldn't try to pick up the item at all.

		enum TryPickUpResult
		{
			SUCCESS,               // The item was picked up.
			CANNOT_HANDLE,         // The item cannot be handled by this owner.
			FAILED                 // The item can be handled by this owner, but failed (inventory full, no one in range, etc.)
		}

		public class ItemOwnerTotalDamagePair
		{
			public IGameStaticItemOwner Owner { get; set; }
			public double Damage { get; set; }

			public ItemOwnerTotalDamagePair() { }

			public ItemOwnerTotalDamagePair(IGameStaticItemOwner owner, double damage)
			{
				Owner = owner;
				Damage = damage;
			}
		}
	}
}
