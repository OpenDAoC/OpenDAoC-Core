using System;
using System.Collections;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// keep door in world
	/// </summary>
	public class GameKeepDoor : GameDoorBase, IKeepItem
	{
		private const int DOOR_CLOSE_THRESHOLD = 15;
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#region properties

		private int m_doorID;
		private int m_oldMaxHealth;
		private byte m_oldHealthPercent;
		private bool m_isPostern;

		private bool m_RelicMessage75;
		private bool m_RelicMessage50;
		private bool m_RelicMessage25;

		/// <summary>
		/// The door index which is unique
		/// </summary>
		public override int DoorID
		{
			get { return m_doorID; }
			set { m_doorID = value; }
		}

		public int OwnerKeepID
		{
			get { return (m_doorID / 100000) % 1000; }
		}

		public int TowerNum
		{
			get { return (m_doorID / 10000) % 10; }
		}

		public int KeepID
		{
			get { return OwnerKeepID + TowerNum * 256; }
		}

		public int ComponentID
		{
			get { return (m_doorID / 100) % 100; }
		}

		public int DoorIndex
		{
			get { return m_doorID % 10; }
		}

		/// <summary>
		/// This flag is send in packet(keep door = 4, regular door = 0)
		/// </summary>
		public override uint Flag
		{
			get
			{
				return 4;
			}
			set { }
		}

		/// <summary>
		/// Get the realm of the keep door from keep owner
		/// </summary>
		public override eRealm Realm
		{
			get
			{
				if (Component == null || Component.Keep == null)
				{
					return eRealm.None;
				}

				return Component.Keep.Realm;
			}
		}

		/// <summary>
		/// door state (open or closed)
		/// </summary>
		protected eDoorState m_state;

		/// <summary>
		/// door state (open or closed)
		/// call the broadcast of state in area
		/// </summary>
		public override eDoorState State
		{
			get => m_state;
			set
			{
				if (m_state != value)
				{
					m_state = value;

					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						player.Out.SendDoorState(CurrentRegion, this);
				}
			}
		}

		/// <summary>
		/// The level of door is keep level now
		/// </summary>
		public override byte Level
		{
			get
			{
				if (Component == null || Component.Keep == null)
				{
					return 0;
				}

				return Component.Keep.Level;
			}
		}

		public bool IsRelic
		{
			get
			{
				return Component.Keep.IsRelic;
			}
		}

		public void UpdateLevel()
		{
			if (MaxHealth != m_oldMaxHealth)
			{
				if (m_oldMaxHealth > 0)
					Health = (int)Math.Ceiling(Health * MaxHealth / (double)m_oldMaxHealth);

				m_oldMaxHealth = MaxHealth;
			}

			SaveIntoDatabase();
		}

		public override bool IsAttackableDoor
		{
			get
			{				
				if (Component == null || Component.Keep == null)
					return false;

                if (Component.Keep is GameKeepTower)
				{
					if (DoorIndex == 1)
						return true;
				}
				else if (Component.Keep is GameKeep or RelicGameKeep)
				{
					return !m_isPostern;
				}
                return false;
			}
		}

		public override int Health
		{
			get
			{
				if (!IsAttackableDoor)
					return 0;
				return base.Health;
			}
			set
			{
				base.Health = value;

				if (HealthPercent > DOOR_CLOSE_THRESHOLD && m_state == eDoorState.Open)
				{
					CloseDoor();
				}
			}
		}

		public override int RealmPointsValue
		{
			get
			{
				return 0;
			}
		}

		public override long ExperienceValue
		{
			get
			{
				return 0;
			}
		}

		public override string Name
		{
			get
			{
				string name = "";

				if (IsAttackableDoor)
				{
					name = IsRelic ? "Relic Gate" : "Keep Door";
				}
				else
				{
					name = "Postern Door";
				}

				if (ServerProperties.Properties.ENABLE_DEBUG)
				{
					name += " ( C:" + ComponentID + " T:" + TemplateID + ")";

				}

				return name;
			}
		}

		protected string m_templateID;
		public string TemplateID
		{
			get { return m_templateID; }
		}

		protected GameKeepComponent m_component;
		public GameKeepComponent Component
		{
			get { return m_component; }
			set { m_component = value; }
		}

		protected DbKeepPosition m_position;
		public DbKeepPosition Position
		{
			get { return m_position; }
			set { m_position = value; }
		}

		#endregion

		#region function override

		/// <summary>
		/// Procs don't normally fire on game keep components
		/// </summary>
		/// <param name="ad"></param>
		/// <param name="weapon"></param>
		/// <returns></returns>
		public override bool AllowWeaponMagicalEffect(AttackData ad, DbInventoryItem weapon, Spell weaponSpell)
		{
			if (weapon.Flags == 10) //Bruiser or any other item needs Itemtemplate "Flags" set to 10 to proc on keep components
				return true;
			else return false; // special code goes here
		}


		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (damageAmount > 0 && IsAlive)
			{
				Component.Keep.LastAttackedByEnemyTick = CurrentRegion.Time;
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);

				//only on hp change
				if (m_oldHealthPercent != HealthPercent)
				{
					m_oldHealthPercent = HealthPercent;

					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						ClientService.UpdateObjectForPlayer(player, this);
				}
			}

			if (!IsRelic) return;
			
			if (HealthPercent == 25)
			{
				if (!m_RelicMessage25)
				{
					m_RelicMessage25 = true;
					BroadcastRelicGateDamage();
				}
			}

			if (HealthPercent == 50)
			{
				if (!m_RelicMessage50)
				{
					m_RelicMessage50 = true;
					BroadcastRelicGateDamage();
				}
			}

			if (HealthPercent == 75)
			{
				if (!m_RelicMessage75)
				{
					m_RelicMessage75 = true;
					BroadcastRelicGateDamage();
				}
			}
		}

		private void BroadcastRelicGateDamage()
		{
			string message = $"{Component.Keep.Name} is under attack!";

			foreach (GamePlayer player in ClientService.GetPlayersOfRealm(Realm))
			{
				player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
				player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}

			if (Properties.DISCORD_ACTIVE && !string.IsNullOrEmpty(Properties.DISCORD_RVR_WEBHOOK_ID))
				GameRelicPad.BroadcastDiscordRelic(message, Realm, Component.Keep.Name);
		}

		public override void ModifyAttack(AttackData attackData)
		{
			// Allow a GM to use commands to damage components, regardless of toughness setting
			if (attackData.DamageType == eDamageType.GM)
				return;

			int toughness = Properties.SET_KEEP_DOOR_TOUGHNESS;
			int baseDamage = attackData.Damage;
			int styleDamage = attackData.StyleDamage;
			int criticalDamage = 0;

			GameLiving source = attackData.Attacker;

			if (Component.Keep is GameKeepTower)
			{
				toughness = Properties.SET_TOWER_DOOR_TOUGHNESS;
			}

			if (Component.Keep.KeepID == 11) //Reduce toughness for Thid CK
			{
				toughness = 25; //Our "normal" toughness is 10% for OF keeps, increasing damage on Thid CK doors
			}

			if (source is GamePlayer)
			{
				baseDamage = (baseDamage - (baseDamage * 5 * Component.Keep.Level / 100)) * toughness / 100;
				styleDamage = (styleDamage - (styleDamage * 5 * Component.Keep.Level / 100)) * toughness / 100;
			}
			else if (source is GameNPC)
			{
				if (!Properties.DOORS_ALLOWPETATTACK)
				{
					baseDamage = 0;
					styleDamage = 0;
					attackData.AttackResult = eAttackResult.NotAllowed_ServerRules;
				}
				else
				{
					baseDamage = (baseDamage - (baseDamage * 5 * Component.Keep.Level / 100)) * toughness / 100;
					styleDamage = (styleDamage - (styleDamage * 5 * Component.Keep.Level / 100)) * toughness / 100;

					if (((GameNPC)source).Brain is AI.Brain.IControlledBrain)
					{
						GamePlayer player = (((AI.Brain.IControlledBrain)((GameNPC)source).Brain).Owner as GamePlayer);
						if (player != null)
						{
							// special considerations for pet spam classes
							if (player.CharacterClass.ID == (int)eCharacterClass.Theurgist || player.CharacterClass.ID == (int)eCharacterClass.Animist)
							{
								baseDamage = (int)(baseDamage * Properties.PET_SPAM_DAMAGE_MULTIPLIER);
								styleDamage = (int)(styleDamage * Properties.PET_SPAM_DAMAGE_MULTIPLIER);
							}
							else
							{
								baseDamage = (int)(baseDamage * Properties.PET_DAMAGE_MULTIPLIER);
								styleDamage = (int)(styleDamage * Properties.PET_DAMAGE_MULTIPLIER);
							}
						}
					}
				}
			}

			attackData.Damage = baseDamage;
			attackData.StyleDamage = styleDamage;
			attackData.CriticalDamage = criticalDamage;
		}


		/// <summary>
		/// This function is called from the ObjectInteractRequestHandler
		/// It teleport player in the keep if player and keep have the same realm
		/// </summary>
		/// <param name="player">GamePlayer that interacts with this object</param>
		/// <returns>false if interaction is prevented</returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			if (player.IsMezzed)
			{
				player.Out.SendMessage("You are mesmerized!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.IsStunned)
			{
				player.Out.SendMessage("You are stunned!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}


			if (!GameServer.KeepManager.IsEnemy(this, player) || player.Client.Account.PrivLevel != 1)
			{
				int keepz = Z, distance = 0;

				//calculate distance
				//normal door
				if (DoorIndex == 1)
					distance = 150;
				//side or internal door
				else
					distance = 100;

				//calculate Z
				if (Component.Keep is GameKeepTower && !Component.Keep.IsPortalKeep)
				{
					//when entering a tower, we need to raise Z
					//portal keeps are considered towers too, so we check component count
					if (IsObjectInFront(player, 180))
					{
						if (DoorID == 1)
						{
							keepz = Z + 83;
						}
						else
						{
							distance = 150;
						}
					}
				}
				else
				{
					//when entering a keeps inner door, we need to raise Z
					if (IsObjectInFront(player, 180))
					{
						//To find out if a door is the keeps inner door, we compare the distance between
						//the component for the keep and the component for the gate
						int keepdistance = int.MaxValue;
						int gatedistance = int.MaxValue;
						foreach (GameKeepComponent c in Component.Keep.KeepComponents)
						{
							if ((GameKeepComponent.eComponentSkin)c.Skin == GameKeepComponent.eComponentSkin.Keep)
							{
								keepdistance = GetDistanceTo(c);
							}
							if ((GameKeepComponent.eComponentSkin)c.Skin == GameKeepComponent.eComponentSkin.Gate)
							{
								gatedistance = GetDistanceTo(c);
							}
							//when these are filled we can stop the search
							if (keepdistance != int.MaxValue && gatedistance != int.MaxValue)
								break;
						}
						if (DoorIndex == 1 && keepdistance < gatedistance)
							keepz = Z + 92;//checked in game with lvl 1 keep
					}
				}

                Point2D keepPoint;
				//calculate x y
				if (IsObjectInFront(player, 180))
					keepPoint = GetPointFromHeading(Heading, -distance );
				else
					keepPoint = GetPointFromHeading(Heading, distance );

				//move player
				player.MoveTo(CurrentRegionID, keepPoint.X, keepPoint.Y, keepz, player.Heading);
			}
			return base.Interact(player);
		}

		public override IList GetExamineMessages(GamePlayer player)
		{
			/*
			 * You select the Keep Gate. It belongs to your realm.
			 * You target [the Keep Gate]
			 * 
			 * You select the Keep Gate. It belongs to an enemy realm and can be attacked!
			 * You target [the Keep Gate]
			 * 
			 * You select the Postern Door. It belongs to an enemy realm!
			 * You target [the Postern Door]
			 */

			IList list = base.GetExamineMessages(player);
			string text = "You select the " + Name + ".";
			if (!GameServer.KeepManager.IsEnemy(this, player))
			{
				text = text + " It belongs to your realm.";
			}
			else
			{
				if (IsAttackableDoor)
				{
					text = text + " It belongs to an enemy realm and can be attacked!";
				}
				else
				{
					text = text + " It belongs to an enemy realm!";
				}
			}

			list.Add(text);

			ChatUtil.SendDebugMessage(player, "Health = " + Health);

			if (IsAttackableDoor)
			{
				// Attempt to fix issue where some players see door as closed when it should be broken open
				// if you target a door it will re-broadcast it's state

				if (Health <= 0 && State != eDoorState.Open)
					State = eDoorState.Open;

				ClientService.UpdateObjectForPlayer(player, this);
			}

			return list;
		}

		public override string GetName(int article, bool firstLetterUppercase)
		{
			return "the " + base.GetName(article, firstLetterUppercase);
		}

		/// <summary>
		/// Starts the power regeneration
		/// </summary>
		public override void StartPowerRegeneration()
		{
			// No regeneration for doors
			return;
		}
		/// <summary>
		/// Starts the endurance regeneration
		/// </summary>
		public override void StartEnduranceRegeneration()
		{
			// No regeneration for doors
			return;
		}

		public override void StartHealthRegeneration()
		{
			if (!IsAttackableDoor)
				return;

			if ((m_repairTimer != null && m_repairTimer.IsAlive) || Health >= MaxHealth)
				return;

			m_repairTimer = new AuxECSGameTimer(this);
			m_repairTimer.Callback = new AuxECSGameTimer.AuxECSTimerCallback(RepairTimerCallback);
			m_repairTimer.Start(REPAIR_INTERVAL);
			m_repairTimer.StartTick = GameLoop.GetCurrentTime() + REPAIR_INTERVAL; // Skip the first tick to avoid repairing on server start.
		}

		public void DeleteObject()
		{
			RemoveTimers();

			if (Component != null)
			{
				if (Component.Keep != null)
				{
					Component.Keep.Doors.Remove(ObjectID.ToString());
				}

				Component.Delete();
			}

			Component = null;
			Position = null;
			base.Delete();
			CurrentRegion = null;
		}

		public virtual void RemoveTimers()
		{
			if (m_repairTimer != null)
			{
				m_repairTimer.Stop();
				m_repairTimer = null;
			}

		}
		#endregion

		#region Save/load DB

		/// <summary>
		/// save the keep door object in DB
		/// </summary>
		public override void SaveIntoDatabase()
		{
			if (InternalID == null)
				return;

			DbDoor dbDoor = GameServer.Database.FindObjectByKey<DbDoor>(InternalID);

			if (dbDoor == null)
				return;

			dbDoor.Health = Health;
			dbDoor.State = (int)State;
			GameServer.Database.SaveObject(dbDoor);
		}

		/// <summary>
		/// load the keep door object from DB object
		/// </summary>
		/// <param name="obj"></param>
		public override void LoadFromDatabase(DataObject obj)
		{
			DbDoor dbDoor = obj as DbDoor;
			if (dbDoor == null)
				return;

			base.LoadFromDatabase(obj);

			Zone curZone = WorldMgr.GetZone((ushort)(dbDoor.InternalID / 1000000));
			if (curZone == null)
				return;
			
			CurrentRegion = curZone.ZoneRegion;
			m_name = dbDoor.Name;
			m_health = dbDoor.Health;
			_heading = (ushort)dbDoor.Heading;
			m_x = dbDoor.X;
			m_y = dbDoor.Y;
			m_z = dbDoor.Z;
			m_level = 0;
			m_model = 0xFFFF;
			m_doorID = dbDoor.InternalID;
			m_isPostern = dbDoor.IsPostern;

			AddToWorld();

			foreach (AbstractArea area in CurrentAreas)
			{
				if (area is KeepArea keepArea)
				{
					string sKey = dbDoor.InternalID.ToString();
					if (!keepArea.Keep.Doors.ContainsKey(sKey))
					{
						Component = new GameKeepComponent();
						Component.Keep = keepArea.Keep;
						keepArea.Keep.Doors.Add(sKey, this);
					}
					break;
				}
			}

			// HealthPercent relies on MaxHealth, which returns 0 if used before adding the door to the world and setting Component.Keep
			// Keep doors are always closed if they have more than DOOR_CLOSE_THRESHOLD% health. Otherwise the value is retrieved from the DB.
			// Postern doors are always closed.
			m_state = m_isPostern || HealthPercent > DOOR_CLOSE_THRESHOLD ? eDoorState.Closed : (eDoorState)Enum.ToObject(typeof(eDoorState), dbDoor.State);

			StartHealthRegeneration();
			DoorMgr.RegisterDoor(this);
		}

		public virtual void LoadFromPosition(DbKeepPosition pos, GameKeepComponent component)
		{
			m_templateID = pos.TemplateID;
			m_component = component;

			PositionMgr.LoadKeepItemPosition(pos, this);
			component.Keep.Doors[m_templateID] = this;

			m_oldMaxHealth = MaxHealth;
			m_health = MaxHealth;
			m_name = "Keep Door";
			m_oldHealthPercent = HealthPercent;
			m_doorID = GenerateDoorID();
			m_model = 0xFFFF;
			m_state = eDoorState.Closed;

			if (AddToWorld())
			{
				StartHealthRegeneration();
				DoorMgr.RegisterDoor(this);
			}
			else
			{
				log.Error("Failed to load keep door from keepposition_id =" + pos.ObjectId + ". Component SkinID=" + component.Skin + ". KeepID=" + component.Keep.KeepID);
			}
		}

		public void MoveToPosition(DbKeepPosition position) { }

		public int GenerateDoorID()
		{
			int doortype = 7;
			int ownerKeepID = 0;
			int towerIndex = 0;

			if (m_component.Keep is GameKeepTower)
			{
				GameKeepTower tower = m_component.Keep as GameKeepTower;

				if (tower.Keep != null)
				{
					ownerKeepID = tower.Keep.KeepID;
				}
				else
				{
					ownerKeepID = tower.OwnerKeepID;
				}

				towerIndex = tower.KeepID >> 8;
			}
			else
			{
				ownerKeepID = m_component.Keep.KeepID;
			}

			int componentID = m_component.ID;

			//index not sure yet
			int doorIndex = Position.TemplateType;
			int id = 0;
			//add door type
			id += doortype * 100000000;
			id += ownerKeepID * 100000;
			id += towerIndex * 10000;
			id += componentID * 100;
			id += doorIndex;
			return id;
		}
		#endregion

		/// <summary>
		/// call when player try to open door
		/// </summary>
		public override void Open(GameLiving opener = null)
		{
			//do nothing because gamekeep must be destroyed to be open
		}
		/// <summary>
		/// call when player try to close door
		/// </summary>
		public override void Close(GameLiving closer = null)
		{
			//do nothing because gamekeep must be destroyed to be open
		}

		/// <summary>
		/// This function is called when door "die" to open door
		/// </summary>
		public override void Die(GameObject killer)
		{
			base.Die(killer);

			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
				player.Out.SendMessage($"The {Name} is broken!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

			m_state = eDoorState.Open;
			BroadcastDoorStatus();
			SaveIntoDatabase();
		}

		/// <summary>
		/// This method is called when door is repair or keep is reset
		/// </summary>
		public virtual void CloseDoor()
		{
			m_state = eDoorState.Closed;
			BroadcastDoorStatus();
		}

		/// <summary>
		/// boradcast the door status to all player near the door
		/// </summary>
		public virtual void BroadcastDoorStatus()
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				ClientService.UpdateObjectForPlayer(player, this);
				player.Out.SendDoorState(CurrentRegion, this);
			}
		}

		protected AuxECSGameTimer m_repairTimer;
		protected const int REPAIR_INTERVAL = 30 * 60 * 1000;

		public int RepairTimerCallback(AuxECSGameTimer timer)
		{
			if (Component == null || Component.Keep == null)
				return 0;

			if (HealthPercent == 100)
				return 0;
			else if (!Component.Keep.InCombat)
				Repair(MaxHealth / 100 * 5);

			return REPAIR_INTERVAL;
		}

		/// <summary>
		/// This Function is called when door has been repaired
		/// </summary>
		/// <param name="amount">how many HP is repaired</param>
		public void Repair(int amount)
		{
			Health += amount;

			if (HealthPercent > 25)
				m_RelicMessage25 = false;
			if (HealthPercent > 50)
				m_RelicMessage50 = false;
			if (HealthPercent > 75)
				m_RelicMessage75 = false;

			BroadcastDoorStatus();
			SaveIntoDatabase();
		}
		/// <summary>
		/// This Function is called when keep is taken to repair door
		/// </summary>
		/// <param name="realm">new realm of keep taken</param>
		public void Reset(eRealm realm)
		{
			Realm = realm;
			Health = MaxHealth;
			m_oldHealthPercent = HealthPercent;
			m_RelicMessage25 = false;
			m_RelicMessage50 = false;
			m_RelicMessage75 = false;
			CloseDoor();
			SaveIntoDatabase();
		}

		/*
		 * Note that 'enter' and 'exit' commands will also work at these doors.
		 */

		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str))
				return false;

			if (source is GamePlayer == false)
				return false;

			str = str.ToLower();

			if (str.Contains("enter") || str.Contains("exit"))
				Interact(source as GamePlayer);
			return true;
		}

		public override bool SayReceive(GameLiving source, string str)
		{
			if (!base.SayReceive(source, str))
				return false;

			if (source is GamePlayer == false)
				return false;

			str = str.ToLower();

			if (str.Contains("enter") || str.Contains("exit"))
				Interact(source as GamePlayer);
			return true;
		}

		public override void NPCManipulateDoorRequest(GameNPC npc, bool open) { }
	}
}
