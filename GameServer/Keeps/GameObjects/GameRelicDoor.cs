using System.Collections;
using Core.Database;
using Core.GS.PacketHandler;

namespace Core.GS.Keeps
{
	public class GameRelicDoor : GameDoorBase
	{
		#region properties

		private int m_doorID;
		/// <summary>
		/// The door index which is unique
		/// </summary>
		public override int DoorID
		{
			get { return m_doorID; }
			set { m_doorID = value; }
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
		/// door state (open or closed)
		/// </summary>
		private EDoorState m_state;

		/// <summary>
		/// door state (open or closed)
		/// call the broadcast of state in area
		/// </summary>
		public override EDoorState State
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

		public override int Health
		{
			get
			{
				return MaxHealth;
			}
			set
			{
			}
		}

		public override string Name
		{
			get
			{
				return "Relic Gate";
			}
		}

		#endregion

		#region function override

		/// <summary>
		/// This methode is override to remove XP system
		/// </summary>
		/// <param name="source">the damage source</param>
		/// <param name="damageType">the damage type</param>
		/// <param name="damageAmount">the amount of damage</param>
		/// <param name="criticalAmount">the amount of critical damage</param>
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			return;
		}

		public override int ChangeHealth(GameObject changeSource, EHealthChangeType healthChangeType, int changeAmount)
		{
			return 0;
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
				player.Out.SendMessage("You are mesmerized!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.IsStunned)
			{
				player.Out.SendMessage("You are stunned!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}


			if (GameServer.ServerRules.IsSameRealm(player, this, true) || player.Client.Account.PrivLevel != 1)
			{
                Point2D point;
				//calculate x y
                if (IsObjectInFront(player, 180) )
                    point = this.GetPointFromHeading(this.Heading, -500);
                else
                    point = this.GetPointFromHeading(this.Heading, 500);

				//move player
				player.MoveTo(CurrentRegionID, point.X, point.Y, player.Z, player.Heading);
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
			if (this.Realm == player.Realm)
				text = text + " It belongs to your realm.";
			else
			{
				text = text + " It belongs to an enemy realm!";
			}
			list.Add(text);
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
			//No regeneration for doors
			return;
		}
		/// <summary>
		/// Starts the endurance regeneration
		/// </summary>
		public override void StartEnduranceRegeneration()
		{
			//No regeneration for doors
			return;
		}

		public override void StartHealthRegeneration()
		{
			return;
		}
		#endregion

		#region Save/load DB

		/// <summary>
		/// save the keep door object in DB
		/// </summary>
		public override void SaveIntoDatabase()
		{

		}

		/// <summary>
		/// load the keep door object from DB object
		/// </summary>
		/// <param name="obj"></param>
		public override void LoadFromDatabase(DataObject obj)
		{
			DbDoor door = obj as DbDoor;
			if (door == null)
				return;
			base.LoadFromDatabase(obj);

			Zone curZone = WorldMgr.GetZone((ushort)(door.InternalID / 1000000));
			if (curZone == null) return;
			this.CurrentRegion = curZone.ZoneRegion;
			m_name = door.Name;
			_heading = (ushort)door.Heading;
			m_x = door.X;
			m_y = door.Y;
			m_z = door.Z;
			m_level = 0;
			m_model = 0xFFFF;
			m_doorID = door.InternalID;
			m_state = EDoorState.Closed;
			this.AddToWorld();

			m_health = MaxHealth;
			StartHealthRegeneration();
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

		public virtual void OpenDoor()
		{
			m_state = EDoorState.Open;
			BroadcastDoorStatus();
		}

		/// <summary>
		/// This method is called when door is repair or keep is reset
		/// </summary>
		public virtual void CloseDoor()
		{
			m_state = EDoorState.Closed;
			BroadcastDoorStatus();
		}

		/// <summary>
		/// boradcast the door statut to all player near the door
		/// </summary>
		public virtual void BroadcastDoorStatus()
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				ClientService.UpdateObjectForPlayer(player, this);
				player.Out.SendDoorState(CurrentRegion, this);
			}
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

		public override void NPCManipulateDoorRequest(GameNpc npc, bool open) { }
	}
}
