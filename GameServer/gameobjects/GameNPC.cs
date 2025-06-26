using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.GS.ServerProperties;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// This class is the baseclass for all Non Player Characters like
	/// Monsters, Merchants, Guards, Steeds ...
	/// </summary>
	public class GameNPC : GameLiving, ITranslatableObject
	{
		public static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
		private static ConcurrentDictionary<Type, Func<AbstractQuest>> _abstractQuestConstructorCache = new();

		private const int VISIBLE_TO_PLAYER_SPAN = 60000;

		public override eGameObjectType GameObjectType => eGameObjectType.NPC;

		#region Formations/Spacing

		//Space/Offsets used in formations
		// Normal = 1
		// Big = 2
		// Huge = 3
		private byte m_formationSpacing = 1;

		/// <summary>
		/// The Minions's x-offset from it's commander
		/// </summary>
		public byte FormationSpacing
		{
			get { return m_formationSpacing; }
			set
			{
				//BD range values vary from 1 to 3.  It is more appropriate to just ignore the
				//incorrect values than throw an error since this isn't a very important area.
				if (value > 0 && value < 4)
					m_formationSpacing = value;
			}
		}

		/// <summary>
		/// Used for that formation type if a GameNPC has a formation
		/// </summary>
		public enum eFormationType
		{
			// M = owner
			// x = following npcs
			//Line formation
			// M x x x
			Line,
			//Triangle formation
			//		x
			// M x
			//		x
			Triangle,
			//Protect formation
			//		 x
			// x  M
			//		 x
			Protect,
		}

		private eFormationType m_formation = eFormationType.Line;
		/// <summary>
		/// How the minions line up with the commander
		/// </summary>
		public eFormationType Formation
		{
			get { return m_formation; }
			set { m_formation = value; }
		}

		#endregion

		#region Sizes/Properties
		/// <summary>
		/// Holds the size of the NPC
		/// </summary>
		protected byte m_size;
		/// <summary>
		/// Gets or sets the size of the npc
		/// </summary>
		public byte Size
		{
			get { return m_size; }
			set
			{
				m_size = value;
				if (ObjectState == eObjectState.Active)
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						player.Out.SendModelAndSizeChange(this, Model, value);
					//					BroadcastUpdate();
				}
			}
		}

		public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier
		{
			get { return LanguageDataObject.eTranslationIdentifier.eNPC; }
		}

		/// <summary>
		/// Holds the translation id.
		/// </summary>
		protected string m_translationId = string.Empty;

		/// <summary>
		/// Gets or sets the translation id.
		/// </summary>
		public string TranslationId
		{
			get { return m_translationId; }
			set { m_translationId = (value == null ? "" : value); }
		}

		/// <summary>
		/// Gets or sets the model of this npc
		/// </summary>
		public override ushort Model
		{
			get { return base.Model; }
			set
			{
				base.Model = value;
				if (ObjectState == eObjectState.Active)
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						player.Out.SendModelChange(this, Model);
				}
			}
		}

		public override ushort Heading
		{
			get => base.Heading;
			set
			{
				if (IsTurningDisabled)
					return;

				base.Heading = value;
			}
		}

		/// <summary>
		/// Gets or sets the level of this NPC
		/// </summary>
		public override byte Level
		{
			get => base.Level;
			set
			{
				base.Level = value;
				AutoSetStats();

				if (m_health > MaxHealth)
					m_health = MaxHealth;
			}
		}

		/// <summary>
		/// Auto set stats based on DB entry, npcTemplate, and level.
		/// </summary>
		/// <param name="dbMob">Mob DB entry to load stats from, retrieved from DB if null</param>
		public virtual void AutoSetStats(DbMob dbMob = null)
		{
			// Don't set stats for mobs until their level is set
			if (Level < 1)
				return;

			// We have to check both the DB and template values to account for mobs changing levels.
			// Otherwise, high level mobs retain their stats when their level is lowered by a GM.
			if (NPCTemplate != null && NPCTemplate.ReplaceMobValues)
			{
				Strength = NPCTemplate.Strength;
				Constitution = NPCTemplate.Constitution;
				Quickness = NPCTemplate.Quickness;
				Dexterity = NPCTemplate.Dexterity;
				Intelligence = NPCTemplate.Intelligence;
				Empathy = NPCTemplate.Empathy;
				Piety = NPCTemplate.Piety;
				Charisma = NPCTemplate.Charisma;
			}
			else
			{
				DbMob mob = dbMob;

				if (mob == null && !string.IsNullOrEmpty(InternalID))
					// This should only happen when a GM command changes level on a mob with no npcTemplate,
					mob = GameServer.Database.FindObjectByKey<DbMob>(InternalID);

				if (mob != null)
				{
					Strength = mob.Strength;
					Constitution = mob.Constitution;
					Quickness = mob.Quickness;
					Dexterity = mob.Dexterity;
					Intelligence = mob.Intelligence;
					Empathy = mob.Empathy;
					Piety = mob.Piety;
					Charisma = mob.Charisma;
				}
			}

			int levelMinusOne = Level - 1;

			if (Strength < 1)
				Strength = (short) (Properties.MOB_AUTOSET_STR_BASE + levelMinusOne * Properties.MOB_AUTOSET_STR_MULTIPLIER);

			if (Constitution < 1)
				Constitution = (short) (Properties.MOB_AUTOSET_CON_BASE + levelMinusOne * Properties.MOB_AUTOSET_CON_MULTIPLIER);

			if (Quickness < 1)
				Quickness = (short) (Properties.MOB_AUTOSET_QUI_BASE + levelMinusOne * Properties.MOB_AUTOSET_QUI_MULTIPLIER);

			if (Dexterity < 1)
				Dexterity = (short) (Properties.MOB_AUTOSET_DEX_BASE + levelMinusOne * Properties.MOB_AUTOSET_DEX_MULTIPLIER);

			if (Intelligence < 1)
				Intelligence = (short) (Properties.MOB_AUTOSET_INT_BASE + levelMinusOne * Properties.MOB_AUTOSET_INT_MULTIPLIER);

			if (Empathy < 1)
				Empathy = (short) (30 + levelMinusOne);

			if (Piety < 1)
				Piety = (short) (30 + levelMinusOne);

			if (Charisma < 1)
				Charisma = (short) (30 + levelMinusOne);
		}

		/*
		/// <summary>
		/// Gets or Sets the effective level of the Object
		/// </summary>
		public override int EffectiveLevel
		{
			get
			{
				IControlledBrain brain = Brain as IControlledBrain;
				if (brain != null)
					return brain.Owner.EffectiveLevel;
				return base.EffectiveLevel;
			}
		}*/

		/// <summary>
		/// Gets or sets the Realm of this NPC
		/// </summary>
		public override eRealm Realm
		{
			get
			{
				IControlledBrain brain = Brain as IControlledBrain;
				if (brain != null)
					return brain.Owner.Realm; // always realm of the owner
				return base.Realm;
			}
			set
			{
				base.Realm = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateNpcForPlayers(this);
			}
		}

		/// <summary>
		/// Gets or sets the name of this npc
		/// </summary>
		public override string Name
		{
			get { return base.Name; }
			set
			{
				base.Name = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateNpcForPlayers(this);
			}
		}

		/// <summary>
		/// Holds the suffix.
		/// </summary>
		private string m_suffix = string.Empty;
		/// <summary>
		/// Gets or sets the suffix.
		/// </summary>
		public string Suffix
		{
			get { return m_suffix; }
			set
			{
				if (value == null)
					m_suffix = string.Empty;
				else
				{
					if (value == m_suffix)
						return;
					else
						m_suffix = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the guild name
		/// </summary>
		public override string GuildName
		{
			get { return base.GuildName; }
			set
			{
				base.GuildName = value;

				if (ObjectState == eObjectState.Active)
					ClientService.CreateNpcForPlayers(this);
			}
		}

		/// <summary>
		/// Holds the examine article.
		/// </summary>
		private string m_examineArticle = string.Empty;
		/// <summary>
		/// Gets or sets the examine article.
		/// </summary>
		public string ExamineArticle
		{
			get { return m_examineArticle; }
			set
			{
				if (value == null)
					m_examineArticle = string.Empty;
				else
				{
					if (value == m_examineArticle)
						return;
					else
						m_examineArticle = value;
				}
			}
		}

		/// <summary>
		/// Holds the message article.
		/// </summary>
		private string m_messageArticle = string.Empty;
		/// <summary>
		/// Gets or sets the message article.
		/// </summary>
		public string MessageArticle
		{
			get { return m_messageArticle; }
			set
			{
				if (value == null)
					m_messageArticle = string.Empty;
				else
				{
					if (value == m_messageArticle)
						return;
					else
						m_messageArticle = value;
				}
			}
		}

		private Faction m_faction = null;
		/// <summary>
		/// Gets the Faction of the NPC
		/// </summary>
		public Faction Faction
		{
			get { return m_faction; }
			set
			{
				m_faction = value;
			}
		}

		private ArrayList m_linkedFactions;
		/// <summary>
		/// The linked factions for this NPC
		/// </summary>
		public ArrayList LinkedFactions
		{
			get { return m_linkedFactions; }
			set { m_linkedFactions = value; }
		}

		private bool m_isConfused;
		/// <summary>
		/// Is this NPC currently confused
		/// </summary>
		public bool IsConfused
		{
			get { return m_isConfused; }
			set { m_isConfused = value; }
		}

		private ushort m_bodyType;
		/// <summary>
		/// The NPC's body type
		/// </summary>
		public ushort BodyType
		{
			get { return m_bodyType; }
			set { m_bodyType = value; }
		}

		private ushort m_houseNumber;
		/// <summary>
		/// The NPC's current house
		/// </summary>
		public ushort HouseNumber
		{
			get { return m_houseNumber; }
			set { m_houseNumber = value; }
		}
		#endregion

		#region Stats


		/// <summary>
		/// Change a stat value
		/// (delegate to GameNPC)
		/// </summary>
		/// <param name="stat">The stat to change</param>
		/// <param name="val">The new value</param>
		public override void ChangeBaseStat(eStat stat, short val)
		{
			int oldstat = GetBaseStat(stat);
			base.ChangeBaseStat(stat, val);
			int newstat = GetBaseStat(stat);
			GameNPC npc = this;
			if (this != null && oldstat != newstat)
			{
				switch (stat)
				{
					case eStat.STR: npc.Strength = (short)newstat; break;
					case eStat.DEX: npc.Dexterity = (short)newstat; break;
					case eStat.CON: npc.Constitution = (short)newstat; break;
					case eStat.QUI: npc.Quickness = (short)newstat; break;
					case eStat.INT: npc.Intelligence = (short)newstat; break;
					case eStat.PIE: npc.Piety = (short)newstat; break;
					case eStat.EMP: npc.Empathy = (short)newstat; break;
					case eStat.CHR: npc.Charisma = (short)newstat; break;
				}
			}
		}

		/// <summary>
		/// Gets NPC's constitution
		/// </summary>
		public virtual short Constitution
		{
			get
			{
				return m_charStat[eStat.CON - eStat._First];
			}
			set { m_charStat[eStat.CON - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's dexterity
		/// </summary>
		public virtual short Dexterity
		{
			get { return m_charStat[eStat.DEX - eStat._First]; }
			set { m_charStat[eStat.DEX - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's strength
		/// </summary>
		public virtual short Strength
		{
			get { return m_charStat[eStat.STR - eStat._First]; }
			set { m_charStat[eStat.STR - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's quickness
		/// </summary>
		public virtual short Quickness
		{
			get { return m_charStat[eStat.QUI - eStat._First]; }
			set { m_charStat[eStat.QUI - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's intelligence
		/// </summary>
		public virtual short Intelligence
		{
			get { return m_charStat[eStat.INT - eStat._First]; }
			set { m_charStat[eStat.INT - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's piety
		/// </summary>
		public virtual short Piety
		{
			get { return m_charStat[eStat.PIE - eStat._First]; }
			set { m_charStat[eStat.PIE - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's empathy
		/// </summary>
		public virtual short Empathy
		{
			get { return m_charStat[eStat.EMP - eStat._First]; }
			set { m_charStat[eStat.EMP - eStat._First] = value; }
		}

		/// <summary>
		/// Gets NPC's charisma
		/// </summary>
		public virtual short Charisma
		{
			get { return m_charStat[eStat.CHR - eStat._First]; }
			set { m_charStat[eStat.CHR - eStat._First] = value; }
		}
		#endregion

		#region Flags/Position/SpawnPosition/UpdateTick/Tether
		/// <summary>
		/// Various flags for this npc
		/// </summary>
		[Flags]
		public enum eFlags : uint
		{
			/// <summary>
			/// The npc is translucent (like a ghost)
			/// </summary>
			GHOST = 0x01,
			/// <summary>
			/// The npc is stealthed (nearly invisible, like a stealthed player; new since 1.71)
			/// </summary>
			STEALTH = 0x02,
			/// <summary>
			/// The npc doesn't show a name above its head but can be targeted
			/// </summary>
			DONTSHOWNAME = 0x04,
			/// <summary>
			/// The npc doesn't show a name above its head and can't be targeted
			/// </summary>
			CANTTARGET = 0x08,
			/// <summary>
			/// Not in nearest enemyes if different vs player realm, but can be targeted if model support this
			/// </summary>
			PEACE = 0x10,
			/// <summary>
			/// The npc is flying (z above ground permitted)
			/// </summary>
			FLYING = 0x20,
			/// <summary>
			/// npc's torch is lit
			/// </summary>
			TORCH = 0x40,
			/// <summary>
			/// npc is a statue (no idle animation, no target...)
			/// </summary>
			STATUE = 0x80,
			/// <summary>
			/// npc is swimming
			/// </summary>
			SWIMMING = 0x100
		}

		/// <summary>
		/// Holds various flags of this npc
		/// </summary>
		protected eFlags m_flags;
		/// <summary>
		/// Spawn point
		/// </summary>
		protected Point3D m_spawnPoint;
		/// <summary>
		/// Spawn Heading
		/// </summary>
		protected ushort m_spawnHeading;


		/// <summary>
		/// package ID defined form this NPC
		/// </summary>
		protected string m_packageID;

		public string PackageID
		{
			get { return m_packageID; }
			set { m_packageID = value; }
		}

		/// <summary>
		/// The last time this NPC was actually updated to at least one player
		/// </summary> 
		protected long m_lastVisibleToPlayerTick = -VISIBLE_TO_PLAYER_SPAN; // Prevents 'IsVisibleToPlayers' from returning true during the first server tick.

		/// <summary>
		/// Gets or Sets the flags of this npc
		/// </summary>
		public virtual eFlags Flags
		{
			get { return m_flags; }
			set
			{
				eFlags oldflags = m_flags;
				m_flags = value;

				if (ObjectState == eObjectState.Active)
				{
					if (oldflags != m_flags)
						ClientService.CreateNpcForPlayers(this);
				}
			}
		}


		public override bool IsUnderwater
		{
			get { return (m_flags & eFlags.SWIMMING) == eFlags.SWIMMING || base.IsUnderwater; }
		}


		/// <summary>
		/// Shows wether any player sees that mob
		/// we dont need to calculate things like AI if mob is in no way
		/// visible to at least one player
		/// </summary>
		public virtual bool IsVisibleToPlayers
		{
			get { return GameLoop.GameLoopTime - m_lastVisibleToPlayerTick < VISIBLE_TO_PLAYER_SPAN; }
		}

		/// <summary>
		/// Gets or sets the spawnposition of this npc
		/// </summary>
		public virtual Point3D SpawnPoint
		{
			get { return m_spawnPoint; }
			set { m_spawnPoint = value; }
		}

		/// <summary>
		/// Gets or sets the spawnposition of this npc
		/// </summary>
		[Obsolete("Use GameNPC.SpawnPoint")]
		public virtual int SpawnX
		{
			get { return m_spawnPoint.X; }
			set { m_spawnPoint.X = value; }
		}
		/// <summary>
		/// Gets or sets the spawnposition of this npc
		/// </summary>
		[Obsolete("Use GameNPC.SpawnPoint")]
		public virtual int SpawnY
		{
			get { return m_spawnPoint.Y; }
			set { m_spawnPoint.Y = value; }
		}
		/// <summary>
		/// Gets or sets the spawnposition of this npc
		/// </summary>
		[Obsolete("Use GameNPC.SpawnPoint")]
		public virtual int SpawnZ
		{
			get { return m_spawnPoint.Z; }
			set { m_spawnPoint.Z = value; }
		}

		/// <summary>
		/// Gets or sets the spawnheading of this npc
		/// </summary>
		public virtual ushort SpawnHeading
		{
			get { return m_spawnHeading; }
			set { m_spawnHeading = value; }
		}

		/// <summary>
		/// Gets the current X of this living. Don't modify this property
		/// to try to change position of the mob while active. Use the
		/// MoveTo function instead
		/// </summary>
		public override int X
		{
			get
			{
				if (!IsMoving)
					return m_x;

				double movementAmount = MovementElapsedTicks * movementComponent.Velocity.X * 0.001;

				if (!IsDestinationValid)
					return (int) Math.Round(m_x + movementAmount);

				double absMovementAmount = Math.Abs(movementAmount);
				return Math.Abs(Destination.X - m_x) < absMovementAmount ? Destination.X : (int) Math.Round(m_x + movementAmount);
			}
		}

		public int RealX => m_x;

		/// <summary>
		/// Gets the current Y of this NPC. Don't modify this property
		/// to try to change position of the mob while active. Use the
		/// MoveTo function instead
		/// </summary>
		public override int Y
		{
			get
			{
				if (!IsMoving)
					return m_y;

				double movementAmount = MovementElapsedTicks * movementComponent.Velocity.Y * 0.001;

				if (!IsDestinationValid)
					return (int) Math.Round(m_y + movementAmount);

				double absMovementAmount = Math.Abs(movementAmount);
				return Math.Abs(Destination.Y - m_y) < absMovementAmount ? Destination.Y : (int) Math.Round(m_y + movementAmount);
			}
		}

		public int RealY => m_y;

		/// <summary>
		/// Gets the current Z of this NPC. Don't modify this property
		/// to try to change position of the mob while active. Use the
		/// MoveTo function instead
		/// </summary>
		public override int Z
		{
			get
			{
				if (!IsMoving)
					return m_z;

				double movementAmount = MovementElapsedTicks * movementComponent.Velocity.Z * 0.001;

				if (!IsDestinationValid)
					return (int) Math.Round(m_z + movementAmount);

				double absMovementAmount = Math.Abs(movementAmount);
				return Math.Abs(Destination.Z - m_z) < absMovementAmount ? Destination.Z : (int) Math.Round(m_z + movementAmount);
			}
		}

		public int RealZ => m_z;

		/// <summary>
		/// The stealth state of this NPC
		/// </summary>
		public override bool IsStealthed => (Flags & eFlags.STEALTH) != 0;
		public bool WasStealthed { get; private set; } = false;

		public override void OnMaxSpeedChange()
		{
			base.OnMaxSpeedChange();
			movementComponent.RestartCurrentMovement();
		}

		protected int m_tetherRange;

		/// <summary>
		/// The mob's tether range; if mob is pulled farther than this distance
		/// it will return to its spawn point.
		/// if TetherRange > 0 ... the amount is the normal value
		/// if TetherRange less or equal 0 ... no tether check
		/// </summary>
		public int TetherRange
		{
			get { return m_tetherRange; }
			set { m_tetherRange = value; }
		}

		/// <summary>
		/// True, if NPC is out of tether range, false otherwise; if no tether
		/// range is specified, this will always return false.
		/// </summary>
		public bool IsOutOfTetherRange
		{
			get
			{
				if (TetherRange > 0)
				{
					if (this.IsWithinRadius(this.SpawnPoint, TetherRange))
						return false;
					else
						return true;
				}
				else
				{
					return false;
				}
			}
		}

		#endregion

		#region Movement

		public virtual int StickMinimumRange => (int) (MeleeAttackRange * 0.375);
		public virtual int StickMaximumRange => 5000;

		public long LastVisibleToPlayersTickCount => m_lastVisibleToPlayerTick;

		public IPoint3D Destination => movementComponent.Destination;
		public GameObject FollowTarget => movementComponent.FollowTarget;
		public string PathID
		{
			get => movementComponent.PathID;
			set => movementComponent.PathID = value;
		}
		public PathPoint CurrentWaypoint
		{
			get => movementComponent.CurrentWaypoint;
			set => movementComponent.CurrentWaypoint = value;
		}
		public bool IsReturningToSpawnPoint => movementComponent.IsReturningToSpawnPoint;
		public int RoamingRange
		{
			get => movementComponent.RoamingRange;
			set => movementComponent.RoamingRange = value;
		}
		public bool IsMovingOnPath => movementComponent.IsMovingOnPath;
		public bool IsNearSpawn => movementComponent.IsNearSpawn;
		public bool IsDestinationValid => movementComponent.IsDestinationValid;
		public bool IsAtDestination => movementComponent.IsAtDestination;
		public bool CanRoam => movementComponent.CanRoam;

		public virtual void WalkTo(Point3D target, short speed)
		{
			movementComponent.WalkTo(target, speed);
		}

		public virtual void PathTo(Point3D target, short speed)
		{
			movementComponent.PathTo(target, speed);
		}

		public virtual void StopMoving()
		{
			movementComponent.StopMoving();
		}

		public virtual void Follow(GameObject target, int minDistance, int maxDistance)
		{
			movementComponent.Follow(target as GameLiving, minDistance, maxDistance);
		}

		public virtual void StopFollowing()
		{
			movementComponent.StopFollowing();
		}

		public virtual void MoveOnPath(short speed)
		{
			movementComponent.MoveOnPath(speed);
		}

		public virtual void StopMovingOnPath()
		{
			movementComponent.StopMovingOnPath();
		}

		public virtual void ReturnToSpawnPoint(short speed)
		{
			movementComponent.ReturnToSpawnPoint(speed);
		}

		public virtual void CancelReturnToSpawnPoint()
		{
			movementComponent.CancelReturnToSpawnPoint();
		}

		public virtual void Roam(short speed)
		{
			movementComponent.Roam(speed);
		}

		public virtual bool FixedSpeed
		{
			set => movementComponent.FixedSpeed = value;
		}

		public long MovementElapsedTicks => movementComponent.MovementElapsedTicks;

		public long MovementStartTick
		{
			set => movementComponent.MovementStartTick = value;
		}

		public virtual void TurnTo(ushort heading, int duration = 0)
		{
			movementComponent.TurnTo(heading, duration);
		}

		public virtual void TurnTo(int x, int y, int duration = 0)
		{
			movementComponent.TurnTo(x, y, duration);
		}

		public virtual void TurnTo(GameObject target, int duration = 0)
		{
			movementComponent.TurnTo(target, duration);
		}

		#endregion

		#region Inventory/LoadfromDB
		private NpcTemplate m_npcTemplate;
		public NpcTemplate NPCTemplate
		{
			get { return m_npcTemplate; }
			set { m_npcTemplate = value; }
		}

		/// <summary>
		/// Loads the equipment template of this npc
		/// </summary>
		/// <param name="equipmentTemplateID">The template id</param>
		public virtual void LoadEquipmentTemplateFromDatabase(string equipmentTemplateID)
		{
			if (string.IsNullOrEmpty(equipmentTemplateID))
				return;

			// Only load the template if it's a new one.
			if (EquipmentTemplateID != equipmentTemplateID)
			{
				EquipmentTemplateID = equipmentTemplateID;
				GameNpcInventoryTemplate template = new();

				if (template.LoadFromDatabase(EquipmentTemplateID))
					Inventory = template.CloseTemplate();
			}

			InitializeActiveWeaponFromInventory();
		}

		public virtual void InitializeActiveWeaponFromInventory()
		{
			if (Inventory == null)
				return;

			if (Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
				SwitchWeapon(eActiveWeaponSlot.Distance);
			else
			{
				DbInventoryItem twoHand = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
				DbInventoryItem oneHand = Inventory.GetItem(eInventorySlot.RightHandWeapon);

				if (twoHand != null && oneHand != null)
					SwitchWeapon(Util.Chance(50) ? eActiveWeaponSlot.TwoHanded : eActiveWeaponSlot.Standard);
				else if (twoHand != null)
					SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				else if (oneHand != null)
					SwitchWeapon(eActiveWeaponSlot.Standard);
			}
		}

		private bool m_loadedFromScript = true;
		public bool LoadedFromScript
		{
			get { return m_loadedFromScript; }
			set { m_loadedFromScript = value; }
		}

		/// <summary>
		/// Load a npc from the npc template
		/// </summary>
		/// <param name="obj">template to load from</param>
		public override void LoadFromDatabase(DataObject obj)
		{
			if (obj is not DbMob)
				return;

			// Clear cached values in case this is a reload.
			NPCTemplate = null;
			EquipmentTemplateID = null;
			Inventory = null;
			_templateEquipmentIds = null;
			_templateLevels = null;
			_templateModels = null;
			_templateSizes = null;
			Spells = [];
			Styles = [];
			Abilities = [];

			base.LoadFromDatabase(obj);

			m_loadedFromScript = false;
			DbMob dbMob = (DbMob)obj;
			TranslationId = dbMob.TranslationId;
			Name = dbMob.Name;
			Suffix = dbMob.Suffix;
			GuildName = dbMob.Guild;
			ExamineArticle = dbMob.ExamineArticle;
			MessageArticle = dbMob.MessageArticle;
			m_x = dbMob.X;
			m_y = dbMob.Y;
			m_z = dbMob.Z;
			Heading = dbMob.Heading;
			MaxSpeedBase = (short) dbMob.Speed;
			CurrentRegionID = dbMob.Region;
			Realm = (eRealm)dbMob.Realm;
			Model = dbMob.Model;
			Size = dbMob.Size;
			Flags = (eFlags)dbMob.Flags;
			m_packageID = dbMob.PackageID;
			m_level = dbMob.Level;
			AutoSetStats(); // Uses level and npctemplate (should be null at this point).
			m_health = MaxHealth;
			MeleeDamageType = (eDamageType)dbMob.MeleeDamageType;

			if (MeleeDamageType == 0)
				MeleeDamageType = eDamageType.Slash;

			m_activeWeaponSlot = eActiveWeaponSlot.Standard;
			rangeAttackComponent.ActiveQuiverSlot = eActiveQuiverSlot.None;
			m_faction = FactionMgr.GetFactionByID(dbMob.FactionID);

			LoadEquipmentTemplateFromDatabase(dbMob.EquipmentTemplateID);

			if (dbMob.RespawnInterval == -1)
				dbMob.RespawnInterval = 0;

			m_respawnInterval = dbMob.RespawnInterval * 1000;
			PathID = dbMob.PathID;

			if (!string.IsNullOrEmpty(dbMob.Brain))
			{
				try
				{
					ABrain brain = null;
					foreach (Assembly asm in ScriptMgr.GameServerScripts)
					{
						brain = (ABrain) asm.CreateInstance(dbMob.Brain, false);

						if (brain != null)
							break;
					}

					if (brain != null)
						SetOwnBrain(brain);
				}
				catch
				{
					if (log.IsErrorEnabled)
						log.Error($"GameNPC error in LoadFromDatabase: can not instantiate brain of type {dbMob.Brain} for npc {dbMob.ClassType}, name {dbMob.Name}.");
				}
			}

			if (Brain is IOldAggressiveBrain aggroBrain)
			{
				aggroBrain.AggroLevel = dbMob.AggroLevel;
				aggroBrain.AggroRange = dbMob.AggroRange;

				if (aggroBrain.AggroRange == Constants.USE_AUTOVALUES)
				{
					if (Realm == eRealm.None)
					{
						aggroBrain.AggroRange = 400;

						if (!Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
							aggroBrain.AggroRange = 500;

						if (CurrentRegion.IsDungeon)
							aggroBrain.AggroRange = 300;
					}
					else
						aggroBrain.AggroRange = 500;
				}

				if (aggroBrain.AggroLevel == Constants.USE_AUTOVALUES)
				{
					aggroBrain.AggroLevel = 0;

					if (Level > 5)
						aggroBrain.AggroLevel = 30;

					if (!Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
						aggroBrain.AggroLevel = 30;

					if (Realm != eRealm.None)
						aggroBrain.AggroLevel = 60;
				}
			}

			m_race = (short)dbMob.Race;
			m_bodyType = (ushort)dbMob.BodyType;
			m_houseNumber = (ushort)dbMob.HouseNumber;
			RoamingRange = dbMob.RoamingRange;
			m_isCloakHoodUp = dbMob.IsCloakHoodUp;
			m_visibleActiveWeaponSlots = dbMob.VisibleWeaponSlots;
			Gender = (eGender)dbMob.Gender;
			OwnerID = dbMob.OwnerID;

			LoadTemplate(NpcTemplateMgr.GetTemplate(dbMob.NPCTemplateID)); // Returns a random template if multiple with the same ID exist.);
		}

		/// <summary>
		/// Deletes the mob from the database
		/// </summary>
		public override void DeleteFromDatabase()
		{
			if (Brain != null && Brain is IControlledBrain)
			{
				return;
			}

			if (InternalID != null)
			{
				DbMob mob = GameServer.Database.FindObjectByKey<DbMob>(InternalID);
				if (mob != null)
					GameServer.Database.DeleteObject(mob);
			}
		}

		/// <summary>
		/// Saves or updates a NPC in the DB.
		/// </summary>
		public override void SaveIntoDatabase()
		{
			// Do not allow saving in an instanced region.
			if (CurrentRegion.IsInstance)
			{
				LoadedFromScript = true;
				return;
			}

			// Do not allow saving of controlled NPCs.
			if (Brain is IControlledBrain)
				return;

			DbMob mob = null;

			if (InternalID != null)
				mob = GameServer.Database.FindObjectByKey<DbMob>(InternalID);

			if (mob == null)
			{
				if (LoadedFromScript == false)
					mob = new DbMob();
				else
					return;
			}

			mob.TranslationId = TranslationId;
			mob.Name = Name;
			mob.Suffix = Suffix;
			mob.Guild = GuildName;
			mob.ExamineArticle = ExamineArticle;
			mob.MessageArticle = MessageArticle;
			mob.X = X;
			mob.Y = Y;
			mob.Z = Z;
			mob.Heading = Heading;
			mob.Speed = MaxSpeedBase;
			mob.Region = CurrentRegionID;
			mob.Realm = (byte)Realm;
			mob.Model = Model;
			mob.Size = Size;
			mob.Level = Level;

			// Stats
			mob.Constitution = Constitution;
			mob.Dexterity = Dexterity;
			mob.Strength = Strength;
			mob.Quickness = Quickness;
			mob.Intelligence = Intelligence;
			mob.Piety = Piety;
			mob.Empathy = Empathy;
			mob.Charisma = Charisma;

			mob.ClassType = GetType().ToString();
			mob.Flags = (uint) Flags;
			mob.Speed = MaxSpeedBase;
			mob.RespawnInterval = m_respawnInterval / 1000;
			mob.HouseNumber = HouseNumber;
			mob.RoamingRange = RoamingRange;

			if (Brain.GetType().FullName != typeof(StandardMobBrain).FullName)
				mob.Brain = Brain.GetType().FullName;

			if (Brain is IOldAggressiveBrain aggroBrain)
			{
				mob.AggroLevel = aggroBrain.AggroLevel;
				mob.AggroRange = aggroBrain.AggroRange;
			}

			mob.EquipmentTemplateID = EquipmentTemplateID;

			if (m_faction != null)
				mob.FactionID = m_faction.Id;

			mob.MeleeDamageType = (int) MeleeDamageType;

			if (NPCTemplate != null)
				mob.NPCTemplateID = NPCTemplate.TemplateId;
			else
				mob.NPCTemplateID = -1;

			mob.Race = Race;
			mob.BodyType = BodyType;
			mob.PathID = PathID;
			mob.IsCloakHoodUp = m_isCloakHoodUp;
			mob.Gender = (byte)Gender;
			mob.VisibleWeaponSlots = m_visibleActiveWeaponSlots;
			mob.PackageID = PackageID;
			mob.OwnerID = OwnerID;

			if (InternalID == null)
			{
				GameServer.Database.AddObject(mob);
				InternalID = mob.ObjectId;
			}
			else
				GameServer.Database.SaveObject(mob);
		}

		// Cached template data from the last call to `LoadTemplate`.
		private List<string> _templateLevels;
		private List<string> _templateModels;
		private List<string> _templateSizes;
		private List<string> _templateEquipmentIds;

		public virtual void LoadTemplate(INpcTemplate template)
		{
			if (template == null)
				return;

			// Some properties don't have to be reloaded if we're reusing the same template.
			// Some properties are also randomized and will be changed even if the template doesn't change.
			bool isNewTemplate = NPCTemplate != template;

			// We need the level to be set before assigning spells to scale pet spells.
			if (isNewTemplate)
			{
				NPCTemplate = template as NpcTemplate;
				HandleTemplateOnlyProperties();
				HandleLevelFromNewTemplate();
				AutoSetStats();
				HandleSpells();
				HandleStyles();
				HandleAbilities();
			}
			else
			{
				if (HandleLevel())
				{
					// If the level has changed.
					AutoSetStats();
					HandleSpells();
					// Styles and abilities currently don't need to be refreshed.
				}
			}

			// Everything below this point overwrites what is in the mob table.
			if (!template.ReplaceMobValues && !LoadedFromScript)
				return;

			if (isNewTemplate)
				HandleNewMobProperties();

			HandleModel();
			HandleSize();
			HandleInventory();
			HandleBrain();

			void HandleTemplateOnlyProperties()
			{
				TetherRange = template.TetherRange;
				ParryChance = template.ParryChance;
				EvadeChance = template.EvadeChance;
				BlockChance = template.BlockChance;
				LeftHandSwingChance = template.LeftHandSwingChance;
			}

			bool HandleLevelFromNewTemplate()
			{
				if (!string.IsNullOrEmpty(template.Level))
				{
					_templateLevels = Util.SplitCSV(template.Level, true);
					return HandleLevel();
				}

				return false;
			}

			bool HandleLevel()
			{
				if (template.ReplaceMobValues &&
					_templateLevels != null &&
					_templateLevels.Count > 0 &&
					byte.TryParse(_templateLevels[Util.Random(0, _templateLevels.Count - 1)], out byte newLevel))
				{
					if (Level != newLevel)
					{
						Level = newLevel;
						return true;
					}

					return false;
				}

				_templateLevels = null;
				return false;
			}

			void HandleSpells()
			{
				if (template.Spells != null)
					Spells = template.Spells;
			}

			void HandleStyles()
			{
				if (template.Styles != null)
					Styles = template.Styles;
			}

			void HandleAbilities()
			{
				if (template.Abilities != null)
				{
					lock (_abilitiesLock)
					{
						foreach (Ability ab in template.Abilities)
							m_abilities[ab.KeyName] = ab;
					}
				}
			}

			void HandleNewMobProperties()
			{
				TranslationId = template.TranslationId;
				Name = template.Name;
				Suffix = template.Suffix;
				GuildName = template.GuildName;
				ExamineArticle = template.ExamineArticle;
				MessageArticle = template.MessageArticle;
				Faction = FactionMgr.GetFactionByID(template.FactionId);
				Race = (short) template.Race;
				BodyType = template.BodyType;
				MaxSpeedBase = template.MaxSpeed;
				Flags = (eFlags) template.Flags;
				MeleeDamageType = template.MeleeDamageType;
				Gender = template.Gender switch
				{
					1 => eGender.Male,
					2 => eGender.Female,
					_ => eGender.Neutral,
				};

				if (!string.IsNullOrEmpty(template.Model))
					_templateModels = Util.SplitCSV(template.Model, true);
				else
					_templateModels = null;

				if (!string.IsNullOrEmpty(template.Size))
					_templateSizes = Util.SplitCSV(template.Size, true);
				else
					_templateSizes = null;

				if (!string.IsNullOrEmpty(template.Inventory))
					_templateEquipmentIds = Util.SplitCSV(template.Inventory);
				else
					_templateEquipmentIds = null;
			}

			void HandleBrain()
			{
				if (m_ownBrain != null)
				{
					if (m_ownBrain is StandardMobBrain brain)
					{
						brain.AggroLevel = template.AggroLevel;
						brain.AggroRange = template.AggroRange;
					}

					return;
				}

				SetOwnBrain(new StandardMobBrain()
				{
					AggroLevel = template.AggroLevel,
					AggroRange = template.AggroRange,
					Body = this
				});
			}

			void HandleModel()
			{
				if (_templateModels != null && _templateModels.Count > 0 && ushort.TryParse(_templateModels[Util.Random(0, _templateModels.Count - 1)], out ushort model))
					Model = model;
			}

			void HandleSize()
			{
				if (_templateSizes != null && _templateSizes.Count > 0 && byte.TryParse(_templateSizes[Util.Random(0, _templateSizes.Count - 1)], out byte size))
					Size = size;
				else
					Size = 50;
			}

			void HandleInventory()
			{
				if (_templateEquipmentIds == null || _templateEquipmentIds.Count <= 0)
					return;

				GameNpcInventoryTemplate inventoryTemplate = new();
				bool foundEquipment = false;
				List<string> templatedInventory = [];

				// Try to load from the npcequipment table.
				if (!template.Inventory.Contains(':'))
				{
					foreach (string str in _templateEquipmentIds)
						templatedInventory.Add(str);

					string equipmentId = templatedInventory[Util.Random(templatedInventory.Count - 1)];

					if (inventoryTemplate.LoadFromDatabase(equipmentId))
						foundEquipment = true;
				}

				// If that failed, parse it the old way (legacy entry).
				if (!foundEquipment && template.Inventory.Contains(':'))
				{
					List<int> tempModels = [];

					foreach (string str in _templateEquipmentIds)
					{
						tempModels.Clear();
						string[] slotXModels = str.Split(':');

						if (slotXModels.Length == 2)
						{
							if (int.TryParse(slotXModels[0], out int slot))
							{
								foreach (string strModel in slotXModels[1].Split('|').ToList())
								{
									if (ushort.TryParse(strModel, out ushort model))
										tempModels.Add(model);
								}

								if (tempModels.Count > 0)
									foundEquipment |= inventoryTemplate.AddNPCEquipment((eInventorySlot) slot, tempModels[Util.Random(tempModels.Count - 1)]);
							}
						}
					}
				}

				if (foundEquipment)
				{
					Inventory = new GameNPCInventory(inventoryTemplate);
					InitializeActiveWeaponFromInventory();
				}
			}
		}

		public void UpdateNPCEquipmentAppearance()
		{
			if (ObjectState != eObjectState.Active) return;
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendLivingEquipmentUpdate(this);
		}

		/// <summary>
		/// Switches the active weapon to another one
		/// </summary>
		/// <param name="slot">the new eActiveWeaponSlot</param>
		public override void SwitchWeapon(eActiveWeaponSlot slot)
		{
			bool wasInAttackState = attackComponent.AttackState;

			// Stop attack before changing weapon so that animations play correctly.
			if (wasInAttackState)
				attackComponent.StopAttack();

			eActiveWeaponSlot previousActiveWeaponSlot = ActiveWeaponSlot;
			base.SwitchWeapon(slot);

			// Resume attack and notify `attackAction` (disables or reenables automatic ranged weapon switch for NPCs).
			// This is to comply with scripted NPCs and doesn't happen if `StartAttackWithMeleeWeapon` was called instead.
			if (wasInAttackState)
			{
				attackComponent.attackAction.OnForcedWeaponSwitch();
				attackComponent.RequestStartAttack();
			}

			if (previousActiveWeaponSlot != ActiveWeaponSlot)
				BroadcastLivingEquipmentUpdate();
		}

		/// <summary>
		/// Equipment templateID
		/// </summary>
		protected string m_equipmentTemplateID;
		/// <summary>
		/// The equipment template id of this npc
		/// </summary>
		public string EquipmentTemplateID
		{
			get { return m_equipmentTemplateID; }
			set { m_equipmentTemplateID = value; }
		}

		#endregion

		#region Quest
		/// <summary>
		/// Holds all the quests this npc can give to players
		/// </summary>
		protected readonly ArrayList m_questListToGive = new ArrayList();
		protected readonly Lock _questListToGiveLock = new();

		/// <summary>
		/// Gets the questlist of this player
		/// </summary>
		public IList QuestListToGive
		{
			get { return m_questListToGive; }
		}

		/// <summary>
		/// Adds a scripted quest type to the npc questlist
		/// </summary>
		/// <param name="questType">The quest type to add</param>
		/// <returns>true if added, false if the npc has already the quest!</returns>
		public void AddQuestToGive(Type questType)
		{
			lock (_questListToGiveLock)
			{
				if (HasQuest(questType) != null)
					return;

				AbstractQuest newQuest = null;

				try
				{
					newQuest = _abstractQuestConstructorCache.GetOrAdd(questType, (key) => CompiledConstructorFactory.CompileConstructor(key, []) as Func<AbstractQuest>)();
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error(e);
				}

				if (newQuest != null)
					m_questListToGive.Add(newQuest);
			}
		}

		/// <summary>
		/// removes a scripted quest from this npc
		/// </summary>
		/// <param name="questType">The questType to remove</param>
		/// <returns>true if added, false if the npc has already the quest!</returns>
		public bool RemoveQuestToGive(Type questType)
		{
			lock (_questListToGiveLock)
			{
				foreach (AbstractQuest q in m_questListToGive)
				{
					if (q.GetType().Equals(questType))
					{
						m_questListToGive.Remove(q);
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Check if the npc can give the specified quest to a player
		/// Used for scripted quests
		/// </summary>
		/// <param name="questType">The type of the quest</param>
		/// <param name="player">The player who search a quest</param>
		/// <returns>the number of time the quest can be done again</returns>
		public int CanGiveQuest(Type questType, GamePlayer player)
		{
			lock (_questListToGiveLock)
			{
				foreach (AbstractQuest q in m_questListToGive)
				{
					if (q.GetType().Equals(questType) && q.CheckQuestQualification(player) && player.HasFinishedQuest(questType) < q.MaxQuestCount)
					{
						return q.MaxQuestCount - player.HasFinishedQuest(questType);
					}
				}
			}
			return 0;
		}

		/// <summary>
		/// Return the proper indicator for quest
		/// TODO: check when finish indicator is set
		/// * when you have done the NPC quest
		/// * when you are at the last step
		/// </summary>
		/// <param name="questType">Type of quest</param>
		/// <param name="player">player requesting the quest</param>
		/// <returns></returns>
		public eQuestIndicator SetQuestIndicator(Type questType, GamePlayer player)
		{
			if (CanShowOneQuest(player)) return eQuestIndicator.Available;
			if (player.HasFinishedQuest(questType) > 0) return eQuestIndicator.Finish;
			return eQuestIndicator.None;
		}

		protected GameNPC m_teleporterIndicator = null;

		/// <summary>
		/// Should this NPC have an associated teleporter indicator
		/// </summary>
		public virtual bool ShowTeleporterIndicator
		{
			get { return false; }
		}

		/// <summary>
		/// Should the NPC show a quest indicator, this can be overriden for custom handling
		/// Checks both scripted and data quests
		/// </summary>
		/// <param name="player"></param>
		/// <returns>True if the NPC should show quest indicator, false otherwise</returns>
		public virtual eQuestIndicator GetQuestIndicator(GamePlayer player)
		{
			// Available one ?
			if (CanShowOneQuest(player))
				return eQuestIndicator.Available;

			// Finishing one ?
			if (CanFinishOneQuest(player))
				return eQuestIndicator.Finish;

			return eQuestIndicator.None;
		}

		/// <summary>
		/// Check if the npc can show a quest indicator to a player
		/// Checks both scripted and data quests
		/// </summary>
		/// <param name="player">The player to check</param>
		/// <returns>true if yes, false if the npc can give any quest</returns>
		public bool CanShowOneQuest(GamePlayer player)
		{
			// Scripted quests
			lock (_questListToGiveLock)
			{
				foreach (AbstractQuest q in m_questListToGive)
				{
					Type questType = q.GetType();
					int doingQuest = (player.IsDoingQuest(questType) != null ? 1 : 0);
					if (q.CheckQuestQualification(player) && player.HasFinishedQuest(questType) + doingQuest < q.MaxQuestCount)
						return true;
				}
			}

			// Data driven quests
			lock (_dataQuestsLock)
			{
				foreach (DataQuest quest in DataQuestList)
				{
					if (quest.ShowIndicator &&
						quest.CheckQuestQualification(player))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Check if the npc can finish one of DataQuest/RewardQuest Player is doing
		/// This can't be check with AbstractQuest as they don't implement anyway of knowing who is the last target or last step !
		/// </summary>
		/// <param name="player">The player to check</param>
		/// <returns>true if this npc is the last step of one quest, false otherwise</returns>
		public bool CanFinishOneQuest(GamePlayer player)
		{
			foreach (var pair in player.QuestList)
			{
				AbstractQuest quest = pair.Key;

				// Handle Data Quest here.
				if (quest is DataQuest dataQuest && dataQuest.TargetName == Name && (dataQuest.TargetRegion == 0 || dataQuest.TargetRegion == CurrentRegionID))
				{
					switch (dataQuest.StepType)
					{
						case DataQuest.eStepType.DeliverFinish:
						case DataQuest.eStepType.InteractFinish:
						case DataQuest.eStepType.KillFinish:
						case DataQuest.eStepType.WhisperFinish:
						case DataQuest.eStepType.CollectFinish:
							return true;
					}
				}

				// Handle Reward Quest here.
				if (quest is RewardQuest rewardQuest && rewardQuest.QuestGiver == this)
				{
					bool done = true;

					foreach (RewardQuest.QuestGoal goal in rewardQuest.Goals)
						done &= goal.IsAchieved;

					if (done)
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Give a quest a to specific player
		/// used for scripted quests
		/// </summary>
		/// <param name="questType">The quest type</param>
		/// <param name="player">The player that gets the quest</param>
		/// <param name="startStep">The starting quest step</param>
		/// <returns>true if added, false if the player do already the quest!</returns>
		public bool GiveQuest(Type questType, GamePlayer player, int startStep)
		{
			AbstractQuest quest = HasQuest(questType);
			if (quest != null)
			{
				AbstractQuest newQuest = (AbstractQuest)Activator.CreateInstance(questType, new object[] { player, startStep });
				if (newQuest != null && player.AddQuest(newQuest))
				{
					player.Out.SendNPCsQuestEffect(this, GetQuestIndicator(player));
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Checks if this npc already has a specified quest
		/// used for scripted quests
		/// </summary>
		/// <param name="questType">The quest type</param>
		/// <returns>the quest if the npc have the quest or null if not</returns>
		protected AbstractQuest HasQuest(Type questType)
		{
			lock (_questListToGiveLock)
			{
				foreach (AbstractQuest q in m_questListToGive)
				{
					if (q.GetType().Equals(questType))
						return q;
				}
			}
			return null;
		}

		#endregion

		#region Riding
		//NPC's can have riders :-)
		/// <summary>
		/// Holds the rider of this NPC as weak reference
		/// </summary>
		public GamePlayer[] Riders;

		/// <summary>
		/// This function is called when a rider mounts this npc
		/// Since only players can ride NPC's you should use the
		/// GamePlayer.MountSteed function instead to make sure all
		/// callbacks are called correctly
		/// </summary>
		/// <param name="rider">GamePlayer that is the rider</param>
		/// <param name="forced">if true, mounting can't be prevented by handlers</param>
		/// <returns>true if mounted successfully</returns>
		public virtual bool RiderMount(GamePlayer rider, bool forced)
		{
			int exists = RiderArrayLocation(rider);
			if (exists != -1)
				return false;

			rider.MoveTo(CurrentRegionID, X, Y, Z, Heading);
			int slot = GetFreeArrayLocation();

			if (slot == -1)
				return false; //full

			Riders[slot] = rider;
			rider.Steed = this;
			return true;
		}

		/// <summary>
		/// This function is called when a rider mounts this npc
		/// Since only players can ride NPC's you should use the
		/// GamePlayer.MountSteed function instead to make sure all
		/// callbacks are called correctly
		/// </summary>
		/// <param name="rider">GamePlayer that is the rider</param>
		/// <param name="forced">if true, mounting can't be prevented by handlers</param>
		/// <param name="slot">The desired slot to mount</param>
		/// <returns>true if mounted successfully</returns>
		public virtual bool RiderMount(GamePlayer rider, bool forced, int slot)
		{
			int exists = RiderArrayLocation(rider);
			if (exists != -1)
				return false;

			if (Riders[slot] != null)
				return false;

			Riders[slot] = rider;
			rider.Steed = this;
			return true;
		}

		/// <summary>
		/// Called to dismount a rider from this npc.
		/// Since only players can ride NPC's you should use the
		/// GamePlayer.MountSteed function instead to make sure all
		/// callbacks are called correctly
		/// </summary>
		/// <param name="forced">if true, the dismounting can't be prevented by handlers</param>
		/// <param name="player">the player that is dismounting</param>
		/// <returns>true if dismounted successfully</returns>
		public virtual bool RiderDismount(bool forced, GamePlayer player)
		{
			if (Riders.Length <= 0)
				return false;

			int slot = RiderArrayLocation(player);

			if (slot < 0)
				return false;

			Riders[slot] = null;
			player.Steed = null;
			return true;
		}

		/// <summary>
		/// Get a free array location on the NPC
		/// </summary>
		/// <returns></returns>
		public int GetFreeArrayLocation()
		{
			for (int i = 0; i < MAX_PASSENGERS; i++)
			{
				if (Riders[i] == null)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Get the riders array location
		/// </summary>
		/// <param name="player">the player to get location of</param>
		/// <returns></returns>
		public int RiderArrayLocation(GamePlayer player)
		{
			for (int i = 0; i < MAX_PASSENGERS; i++)
			{
				if (Riders[i] == player)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Get the riders slot on the npc
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public int RiderSlot(GamePlayer player)
		{
			int location = RiderArrayLocation(player);
			if (location == -1)
				return location;
			return location + SLOT_OFFSET;
		}

		/// <summary>
		/// The maximum passengers the NPC can take
		/// </summary>
		public virtual int MAX_PASSENGERS
		{
			get { return 1; }
		}

		/// <summary>
		/// The minimum number of passengers required to move
		/// </summary>
		public virtual int REQUIRED_PASSENGERS
		{
			get { return 1; }
		}

		/// <summary>
		/// The slot offset for this NPC
		/// </summary>
		public virtual int SLOT_OFFSET
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets a list of the current riders
		/// </summary>
		public GamePlayer[] CurrentRiders
		{
			get
			{
				List<GamePlayer> list = new List<GamePlayer>(MAX_PASSENGERS);
				for (int i = 0; i < MAX_PASSENGERS; i++)
				{
					if (Riders == null || i >= Riders.Length)
						break;

					GamePlayer player = Riders[i];
					if (player != null)
						list.Add(player);
				}
				return list.ToArray();
			}
		}
		#endregion

		#region Add/Remove/Create/Remove/Update

		public override void OnUpdateOrCreateForPlayer()
		{
			m_lastVisibleToPlayerTick = GameLoop.GameLoopTime;

			if (Brain != null && !Brain.ServiceObjectId.IsSet)
				Brain.Start();
		}

		/// <summary>
		/// Adds the npc to the world
		/// </summary>
		/// <returns>true if the npc has been successfully added</returns>
		public override bool AddToWorld()
		{
			if (!base.AddToWorld())
				return false;

			if (MAX_PASSENGERS > 0)
				Riders = new GamePlayer[MAX_PASSENGERS];

			ClientService.CreateNpcForPlayers(this);
			m_spawnPoint.X = X;
			m_spawnPoint.Y = Y;
			m_spawnPoint.Z = Z;
			m_spawnHeading = Heading;

			Brain?.Start();

			if (Mana <= 0 && MaxMana > 0)
				Mana = MaxMana;
			else if (Mana > 0 && MaxMana > 0 && Mana < MaxMana)
				StartPowerRegeneration();

			if (m_houseNumber > 0 && this is not GameConsignmentMerchant)
			{
				if (log.IsInfoEnabled)
					log.Info("NPC '" + Name + "' added to house " + m_houseNumber);

				CurrentHouse = HouseMgr.GetHouse(m_houseNumber);

				if (CurrentHouse == null)
				{
					if (log.IsWarnEnabled)
						log.Warn("House " + CurrentHouse + " for NPC " + Name + " doesn't exist");
				}
				else if (log.IsInfoEnabled)
					log.Info("Confirmed number: " + CurrentHouse.HouseNumber.ToString());
			}

			if (!InCombat && IsAlive && base.Health < MaxHealth)
				base.Health = MaxHealth;

			BuildAmbientTexts();

			if (GameServer.Instance.ServerStatus == EGameServerStatus.GSS_Open)
				FireAmbientSentence(eAmbientTrigger.spawning, this);

			if (ShowTeleporterIndicator)
			{
				if (m_teleporterIndicator == null)
				{
					m_teleporterIndicator = new GameNPC
					{
						Name = string.Empty,
						Model = 1923,
						X = X,
						Y = Y,
						Z = Z + 1,
						CurrentRegionID = CurrentRegionID,
						Flags = eFlags.PEACE | eFlags.CANTTARGET | eFlags.DONTSHOWNAME | eFlags.FLYING
					};
				}

				m_teleporterIndicator.AddToWorld();
			}

			if (IsStealthed)
				WasStealthed = true;

			return true;
		}

		/// <summary>
		/// Fill the ambient text list for this NPC
		/// </summary>
		protected virtual void BuildAmbientTexts()
		{
			// list of ambient texts
			if (!string.IsNullOrEmpty(Name))
				ambientTexts = GameServer.Instance.NpcManager.AmbientBehaviour[Name];
		}

		/// <summary>
		/// Removes the npc from the world
		/// </summary>
		/// <returns>true if the npc has been successfully removed</returns>
		public override bool RemoveFromWorld()
		{
			if (MAX_PASSENGERS > 0)
			{
				foreach (GamePlayer player in CurrentRiders)
					player.DismountSteed(true);
			}

			if (!base.RemoveFromWorld())
				return false;

			Brain.Stop();
			EffectList.CancelAll();

			if (ShowTeleporterIndicator && m_teleporterIndicator != null)
			{
				m_teleporterIndicator.RemoveFromWorld();
				m_teleporterIndicator = null;
			}

			return true;
		}

		/// <summary>
		/// Move an NPC within the same region without removing from world
		/// </summary>
		/// <param name="regionID"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="heading"></param>
		/// <param name="forceMove">Move regardless of combat check</param>
		/// <returns>true if npc was moved</returns>
		public virtual bool MoveInRegion(ushort regionID, int x, int y, int z, ushort heading, bool forceMove)
		{
			if (m_ObjectState != eObjectState.Active)
				return false;

			if (regionID != CurrentRegionID)
				return false;

			if (forceMove == false)
			{
				if (InCombat)
					return false;

				// Only move pet if it's following the owner.
				if (Brain is ControlledMobBrain controlledBrain && controlledBrain.WalkState != eWalkState.Follow)
					return false;
			}

			Region rgn = WorldMgr.GetRegion(regionID);

			if (rgn == null || rgn.GetZone(x, y) == null)
				return false;

			Notify(GameObjectEvent.MoveTo, this, new MoveToEventArgs(regionID, x, y, z, heading));

			List<GamePlayer> playersInRadius = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);

			m_x = x;
			m_y = y;
			m_z = z;
			Heading = heading;

			// Previous position.
			foreach (GamePlayer player in playersInRadius)
				player.Out.SendObjectRemove(this);

			// New position.
			ClientService.CreateNpcForPlayers(this);
			return true;
		}

		/// <summary>
		/// Marks this object as deleted!
		/// </summary>
		public override void Delete()
		{
			lock (_respawnTimerLock)
			{
				if (m_respawnTimer != null)
				{
					m_respawnTimer.Stop();
					m_respawnTimer = null;
				}
			}

			Brain.Stop();
			StopFollowing();
			TempProperties.RemoveProperty(CHARMED_TICK_PROP);
			base.Delete();
		}

		#endregion

		#region AI

		/// <summary>
		/// Holds the own NPC brain
		/// </summary>
		protected ABrain m_ownBrain;

		/// <summary>
		/// Holds the all added to this npc brains
		/// </summary>
		private ArrayList m_brains = new ArrayList(1);

		/// <summary>
		/// Gets the current brain of this NPC
		/// </summary>
		public ABrain Brain
		{
			get
			{
				ArrayList brains = m_brains;
				if (brains.Count > 0)
					return (ABrain)brains[brains.Count - 1];
				return m_ownBrain;
			}
		}

		/// <summary>
		/// Sets the NPC own brain
		/// </summary>
		/// <param name="brain">The new brain</param>
		/// <returns>The old own brain</returns>
		public virtual ABrain SetOwnBrain(ABrain brain)
		{
			if (brain == null)
				return null;

			if (brain.IsActive)
				throw new ArgumentException("The new brain is already active.", nameof(brain));

			ABrain oldBrain = m_ownBrain;

			oldBrain?.Stop();
			m_ownBrain = brain;
			m_ownBrain.Body = this;
			m_ownBrain.FSM?.SetCurrentState(eFSMStateType.WAKING_UP);
			m_ownBrain.Start();
			return oldBrain;
		}

		/// <summary>
		/// Adds a temporary brain to Npc, last added brain is active
		/// </summary>
		public virtual void AddBrain(ABrain newBrain)
		{
			if (newBrain == null)
				return;

			if (newBrain.IsActive)
				throw new ArgumentException("The new brain is already active.", nameof(newBrain));

			Brain.Stop();

			ArrayList brains = new(m_brains)
			{
				newBrain
			};

			m_brains = brains;
			newBrain.Body = this;
			newBrain.FSM?.SetCurrentState(eFSMStateType.WAKING_UP);
			newBrain.Start();
		}

		/// <summary>
		/// Removes a temporary brain from Npc
		/// </summary>
		/// <param name="removeBrain">The brain to remove</param>
		/// <returns>True if brain was found</returns>
		public virtual bool RemoveBrain(ABrain removeBrain)
		{
			if (removeBrain == null)
				return false;

			ArrayList brains = new ArrayList(m_brains);
			int index = brains.IndexOf(removeBrain);

			if (index < 0)
				return false;

			bool active = brains[index] == Brain;
			if (active)
				removeBrain.Stop();
			brains.RemoveAt(index);
			m_brains = brains;
			if (active)
				Brain.Start();

			return true;
		}
		#endregion

		#region GetAggroLevelString

		/// <summary>
		/// How friendly this NPC is to player
		/// </summary>
		/// <param name="player">GamePlayer that is examining this object</param>
		/// <param name="firstLetterUppercase"></param>
		/// <returns>aggro state as string</returns>
		public virtual string GetAggroLevelString(GamePlayer player, bool firstLetterUppercase)
		{
			string aggroLevelString;
			IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;

			if (Faction != null && aggroBrain != null && aggroBrain.AggroLevel > 0 && aggroBrain.AggroRange > 0)
			{
				if (GameServer.ServerRules.IsSameRealm(this, player, true))
				{
					if (firstLetterUppercase)
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly2");
					else
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
				}
				else
				{
					string translationString = string.Empty;

					switch (Faction.GetStandingToFaction(player))
					{
						case Faction.Standing.AGGRESIVE:
						{
							translationString = "GameNPC.GetAggroLevelString.Aggressive1";
							break;
						}
						case Faction.Standing.HOSTILE:
						{
							translationString = "GameNPC.GetAggroLevelString.Hostile1";
							break;
						}
						case Faction.Standing.NEUTRAL:
						{
							translationString = "GameNPC.GetAggroLevelString.Neutral1";
							break;
						}
						case Faction.Standing.FRIENDLY:
						{
							translationString = "GameNPC.GetAggroLevelString.Friendly1";
							break;
						}
					}

					aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, translationString);
				}
			}
			else
			{
				if (GameServer.ServerRules.IsSameRealm(this, player, true))
				{
					if (firstLetterUppercase)
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly2");
					else
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
				}
				else if (aggroBrain != null && aggroBrain.AggroLevel > 0)
				{
					if (firstLetterUppercase)
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive2");
					else
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive1");
				}
				else
				{
					if (firstLetterUppercase)
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral2");
					else
						aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral1");
				}
			}

			return LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.TowardsYou", aggroLevelString);
		}

		public string GetPronoun(int form, bool capitalize, string lang)
		{
			switch (Gender)
			{
				case eGender.Male:
					switch (form)
					{
						case 1:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Male.Possessive"));
						case 2:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Male.Objective"));
						default:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Male.Subjective"));
					}

				case eGender.Female:
					switch (form)
					{
						case 1:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Female.Possessive"));
						case 2:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Female.Objective"));
						default:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Female.Subjective"));
					}
				default:
					switch (form)
					{
						case 1:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Neutral.Possessive"));
						case 2:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Neutral.Objective"));
						default:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Neutral.Subjective"));
					}
			}
		}

		/// <summary>
		/// Gets the proper pronoun including capitalization.
		/// </summary>
		/// <param name="form">1=his; 2=him; 3=he</param>
		/// <param name="capitalize"></param>
		/// <returns></returns>
		public override string GetPronoun(int form, bool capitalize)
		{
			String language = ServerProperties.Properties.DB_LANGUAGE;

			switch (Gender)
			{
				case eGender.Male:
					switch (form)
					{
						case 1:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Male.Possessive"));
						case 2:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Male.Objective"));
						default:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Male.Subjective"));
					}

				case eGender.Female:
					switch (form)
					{
						case 1:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Female.Possessive"));
						case 2:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Female.Objective"));
						default:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Female.Subjective"));
					}
				default:
					switch (form)
					{
						case 1:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Neutral.Possessive"));
						case 2:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Neutral.Objective"));
						default:
							return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
																					 "GameLiving.Pronoun.Neutral.Subjective"));
					}
			}
		}

		/// <summary>
		/// Adds messages to ArrayList which are sent when object is targeted
		/// </summary>
		/// <param name="player">GamePlayer that is examining this object</param>
		/// <returns>list with string messages</returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list;
			string message;
			string extra = Brain is ScoutMobBrain ? " and is a scout" : null;

			// Message: You examine {0}. {1} is {2}.
			switch (player.Client.Account.Language)
			{
				case "EN":
				{
					list = base.GetExamineMessages(player);
					message = LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameNPC.GetExamineMessages.YouExamine",
						GetName(0, false),
						GetPronoun(0, true),
						GetAggroLevelString(player, false),
						extra);
					break;
				}
				default:
				{
					list = new ArrayList(4);
					message = LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameNPC.GetExamineMessages.YouExamine",
						GetName(0, false, player.Client.Account.Language, this),
						GetPronoun(0, true, player.Client.Account.Language),
						GetAggroLevelString(player, false),
						extra);
					break;
				}
			}

			list.Add(message);
			return list;
		}

		/*		/// <summary>
				/// Pronoun of this NPC in case you need to refer it in 3rd person
				/// http://webster.commnet.edu/grammar/cases.htm
				/// </summary>
				/// <param name="firstLetterUppercase"></param>
				/// <param name="form">0=Subjective, 1=Possessive, 2=Objective</param>
				/// <returns>pronoun of this object</returns>
				public override string GetPronoun(bool firstLetterUppercase, int form)
				{
					// TODO: when mobs will get gender
					if(PlayerCharacter.Gender == 0)
						// male
						switch(form)
						{
							default: // Subjective
								if(firstLetterUppercase) return "He"; else return "he";
							case 1:	// Possessive
								if(firstLetterUppercase) return "His"; else return "his";
							case 2:	// Objective
								if(firstLetterUppercase) return "Him"; else return "him";
						}
					else
						// female
						switch(form)
						{
							default: // Subjective
								if(firstLetterUppercase) return "She"; else return "she";
							case 1:	// Possessive
								if(firstLetterUppercase) return "Her"; else return "her";
							case 2:	// Objective
								if(firstLetterUppercase) return "Her"; else return "her";
						}

					// it
					switch(form)
					{
						// Subjective
						default: if(firstLetterUppercase) return "It"; else return "it";
						// Possessive
						case 1:	if(firstLetterUppercase) return "Its"; else return "its";
						// Objective
						case 2: if(firstLetterUppercase) return "It"; else return "it";
					}
				}*/
		#endregion

		#region Interact/WhisperReceive/SayTo

		/// <summary>
		/// The possible ambient triggers for GameNPC actions (e.g., killing, roaming, dying)
		/// </summary>
		public enum eAmbientTrigger
		{
			spawning,
			dying,
			aggroing,
			fighting,
			roaming,
			killing,
			moving,
			interact,
			seeing
		}

		/// <summary>
		/// The ambient texts
		/// </summary>
		public IList<DbMobXAmbientBehavior> ambientTexts;

		/// <summary>
		/// This function is called from the ObjectInteractRequestHandler
		/// </summary>
		/// <param name="player">GamePlayer that interacts with this object</param>
		/// <returns>false if interaction is prevented</returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			if (!GameServer.ServerRules.IsSameRealm(this, player, true) && Faction != null && Faction.GetStandingToFaction(player) >= Faction.Standing.HOSTILE)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.DirtyLook",
					GetName(0, true, player.Client.Account.Language, this)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

				Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
				return false;
			}

			if (MAX_PASSENGERS > 1)
			{
				string name;

				if (this is GameTaxiBoat)
					name = "boat";
				else if (this is GameSiegeRam)
					name = "ram";
				else
					name = string.Empty;

				if (this is GameSiegeRam && player.Realm != Realm)
				{
					player.Out.SendMessage($"This siege equipment is owned by an enemy realm!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}

				if (RiderSlot(player) != -1)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.AlreadyRiding", name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}

				if (GetFreeArrayLocation() == -1)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.IsFull", name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}

				if (player.IsRiding)
					player.DismountSteed(true);

				if (player.IsOnHorse)
					player.IsOnHorse = false;

				player.MountSteed(this, true);
			}

			FireAmbientSentence(eAmbientTrigger.interact, player);
			return true;
		}

		/// <summary>
		/// ToDo
		/// </summary>
		/// <param name="source"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text))
				return false;

			if (source is not GamePlayer player || source.TargetObject is not GameLiving targetLiving)
				return true;

			if (!text.Equals("task", StringComparison.OrdinalIgnoreCase))
				return true;

			if (KillTask.CheckAvailability(player, targetLiving))
				KillTask.BuildTask(player, targetLiving);
			else if (MoneyTask.CheckAvailability(player, targetLiving))
				MoneyTask.BuildTask(player, targetLiving);
			else if (CraftTask.CheckAvailability(player, targetLiving))
				CraftTask.BuildTask(player, targetLiving);
			else
				return false;

			return true;
		}

		public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
		{
			if (this.DataQuestList.Count > 0)
			{
				foreach (DataQuest quest in DataQuestList)
				{
					quest.Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));
				}
			}
			return base.ReceiveItem(source, item);
		}

		/// <summary>
		/// Format "say" message and send it to target in popup window
		/// </summary>
		/// <param name="target"></param>
		/// <param name="message"></param>
		public virtual void SayTo(GamePlayer target, string message, bool announce = true)
		{
			SayTo(target, eChatLoc.CL_PopupWindow, message, announce);
		}

		/// <summary>
		/// Format "say" message and send it to target
		/// </summary>
		/// <param name="target"></param>
		/// <param name="loc">chat location of the message</param>
		/// <param name="message"></param>
		public virtual void SayTo(GamePlayer target, eChatLoc loc, string message, bool announce = true)
		{
			if (target == null)
				return;

			TurnTo(target, 10000);
			string resultText = LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.Says", GetName(0, true, target.Client.Account.Language, this), message);

			switch (loc)
			{
				case eChatLoc.CL_PopupWindow:
					target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					if (announce)
					{
						Message.ChatToArea(this, LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.SpeaksTo", GetName(0, true, target.Client.Account.Language, this), target.GetName(0, false)), eChatType.CT_System, WorldMgr.SAY_DISTANCE, target);
					}
					break;
				case eChatLoc.CL_ChatWindow:
					target.Out.SendMessage(resultText, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
					break;
				case eChatLoc.CL_SystemWindow:
					target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;
			}
		}
		#endregion

		#region Combat

		/// <summary>
		/// The property that holds charmed tick if any
		/// </summary>
		public const string CHARMED_TICK_PROP = "CharmedTick";

		/// <summary>
		/// The duration of no exp after charmed, in game ticks
		/// </summary>
		public const int CHARMED_NOEXP_TIMEOUT = 60000;

		public virtual void StopAttack()
		{
			attackComponent.StopAttack();
		}

		/// <summary>
		/// Starts a melee attack on a target
		/// </summary>
		/// <param name="target">The object to attack</param>
		public virtual void StartAttack(GameObject target)
		{
			attackComponent.RequestStartAttack(target);
		}

		public void StartAttackWithMeleeWeapon(GameObject target)
		{
			eActiveWeaponSlot newSlot;
			DbInventoryItem rightHandWeapon = Inventory.GetItem(eInventorySlot.RightHandWeapon);
			DbInventoryItem twoHandWeapon = Inventory.GetItem(eInventorySlot.TwoHandWeapon);

			if (twoHandWeapon == null)
				newSlot = eActiveWeaponSlot.Standard;
			else if (rightHandWeapon == null)
				newSlot = eActiveWeaponSlot.TwoHanded;
			else
				newSlot = Util.Chance(50) ? eActiveWeaponSlot.TwoHanded : eActiveWeaponSlot.Standard;

			if (newSlot != ActiveWeaponSlot)
			{
				if (attackComponent.AttackState)
					attackComponent.StopAttack();

				SwitchWeapon(newSlot);
			}

			StartAttack(target);
		}

		public void StartAttackWithRangedWeapon(GameObject target)
		{
			if (ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
			{
				StopFollowing();

				if (attackComponent.AttackState)
					attackComponent.StopAttack();

				SwitchWeapon(eActiveWeaponSlot.Distance);
			}

			StartAttack(target);
		}

		private double damageFactor = 1;
		private int orbsReward = 0;

		public override double GetWeaponSkill(DbInventoryItem weapon)
		{
			double weaponSkill = Math.Max(1, (int) Level) * 2.6 * (1 + 0.01 * (GetWeaponStat(weapon) + 30) / 2);
			return Math.Max(1, weaponSkill * GetModified(eProperty.WeaponSkill) * 0.01);
		}

		/// <summary>
		/// Gets/sets the object health
		/// </summary>
		public override int Health
		{
			get => base.Health;
			set
			{
				base.Health = value;

				// Slow NPCs down when they are hurt.
				if (CurrentSpeed > MaxSpeed)
					OnMaxSpeedChange();
			}
		}

		/// <summary>
		/// npcs can always have mana to cast
		/// </summary>
		public override int Mana => 5000;

		/// <summary>
		/// The Max Mana for this NPC
		/// </summary>
		public override int MaxMana => 1000;

		/// <summary>
		/// The Concentration for this NPC
		/// </summary>
		public override int Concentration => 500;

		/// <summary>
		/// Tests if this MOB should give XP and loot based on the XPGainers
		/// </summary>
		/// <returns>true if it should deal XP and give loot</returns>
		public virtual bool IsWorthReward => Brain is not IControlledBrain && CurrentRegion.Time - CHARMED_NOEXP_TIMEOUT >= TempProperties.GetProperty<long>(CHARMED_TICK_PROP);

		protected void ControlledNPC_Release()
		{
			(ControlledBrain as ControlledMobBrain)?.OnRelease();
		}

		/// <summary>
		/// Called when this living dies
		/// </summary>
		public override void ProcessDeath(GameObject killer)
		{
			try
			{
				Brain?.KillFSM();
				FireAmbientSentence(eAmbientTrigger.dying, killer);

				if (ControlledBrain != null)
					ControlledNPC_Release();

				StopMoving();

				if (killer is GameNPC pet && pet.Brain is IControlledBrain petBrain)
					killer = petBrain.GetPlayerOwner();

				if (killer != null)
				{
					Message.SystemToArea(this, $"{GetName(0, true)} dies!", eChatType.CT_PlayerDied, killer);

					if (killer is GamePlayer player)
						player.Out.SendMessage($"{GetName(0, true)} dies!", eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow);

					// Deal out experience, realm points, loot... Based on server rules.
					GameServer.ServerRules.OnNpcKilled(this, killer);
				}

				Group?.RemoveMember(this);
				base.ProcessDeath(killer);

				lock (XpGainersLock)
				{
					XPGainers.Clear();
				}

				Delete();
				TempProperties.RemoveAllProperties();
				StartRespawn();
			}
			finally
			{
				if (IsBeingHandledByReaperService)
					base.ProcessDeath(killer);
			}
		}

		/// <summary>
		/// Stores the melee damage type of this NPC
		/// </summary>
		protected eDamageType m_meleeDamageType = eDamageType.Slash;

		/// <summary>
		/// Gets or sets the melee damage type of this NPC
		/// </summary>
		public virtual eDamageType MeleeDamageType
		{
			get { return m_meleeDamageType; }
			set { m_meleeDamageType = value; }
		}

		/// <summary>
		/// Stores the NPC evade chance
		/// </summary>
		protected byte m_evadeChance;
		/// <summary>
		/// Stores the NPC block chance
		/// </summary>
		protected byte m_blockChance;
		/// <summary>
		/// Stores the NPC parry chance
		/// </summary>
		protected byte m_parryChance;
		/// <summary>
		/// Stores the NPC left hand swing chance
		/// </summary>
		protected byte m_leftHandSwingChance;

		/// <summary>
		/// Gets or sets the NPC evade chance
		/// </summary>
		public virtual byte EvadeChance
		{
			get { return m_evadeChance; }
			set { m_evadeChance = value; }
		}

		/// <summary>
		/// Gets or sets the NPC block chance
		/// </summary>
		public virtual byte BlockChance
		{
			get
			{
				//When npcs have two handed weapons, we don't want them to block
				if (ActiveWeaponSlot != eActiveWeaponSlot.Standard)
					return 0;

				return m_blockChance;
			}
			set
			{
				m_blockChance = value;
			}
		}

		/// <summary>
		/// Gets or sets the NPC parry chance
		/// </summary>
		public virtual byte ParryChance
		{
			get { return m_parryChance; }
			set { m_parryChance = value; }
		}

		/// <summary>
		/// Gets or sets the NPC left hand swing chance
		/// </summary>
		public byte LeftHandSwingChance
		{
			get { return m_leftHandSwingChance; }
			set { m_leftHandSwingChance = value; }
		}

		/// <summary>
		/// Calculates how many times left hand swings
		/// </summary>
		/// <returns></returns>
		public int CalculateLeftHandSwingCount()
		{
			if (Util.Chance(m_leftHandSwingChance))
				return 1;
			return 0;
		}

		/// <summary>
		/// Checks whether Living has ability to use lefthanded weapons
		/// </summary>
		public bool CanUseLefthandedWeapon
		{
			get => m_leftHandSwingChance > 0;
			set => CanUseLefthandedWeapon = value;
		}

		public override void StartInterruptTimer(int duration, AttackData.eAttackType attackType, GameLiving attacker)
		{
			// Increase substantially the base interrupt timer duration for non player controlled NPCs
			// so that they don't start attacking immediately after the attacker's melee swing interval.
			// It makes repositioning them easier without having to constantly attack them.
			if (attacker != this)
			{
				if (Brain is not IControlledBrain controlledBrain || controlledBrain.GetPlayerOwner() == null)
					duration += 2500;
			}

			base.StartInterruptTimer(duration, attackType, attacker);
		}

		protected override bool CheckRangedAttackInterrupt(GameLiving attacker, AttackData.eAttackType attackType)
		{
			// Immobile NPCs can only be interrupted by their own target, and in melee range.
			if (MaxSpeedBase == 0 && (attacker != TargetObject || !IsWithinRadius(attacker, MeleeAttackRange)))
				return false;

			bool interrupted = base.CheckRangedAttackInterrupt(attacker, attackType);

			if (interrupted)
				attackComponent.attackAction.OnAimInterrupt(attacker);

			return interrupted;
		}

		public override int SelfInterruptDurationOnMeleeAttack => AttackSpeed(ActiveWeapon) / 2;

		/// <summary>
		/// The time to wait before each mob respawn
		/// </summary>
		protected int m_respawnInterval = -1;
		/// <summary>
		/// A timer that will respawn this mob
		/// </summary>
		protected ECSGameTimer m_respawnTimer;
		/// <summary>
		/// The sync object for respawn timer modifications
		/// </summary>
		protected readonly Lock _respawnTimerLock = new();

		/// <summary>
		/// The Respawn Interval of this mob in milliseconds
		/// </summary>
		public virtual int RespawnInterval
		{
			get
			{
				if (m_respawnInterval > 0 || m_respawnInterval < 0)
					return m_respawnInterval;

				int minutes = Util.Random(ServerProperties.Properties.NPC_MIN_RESPAWN_INTERVAL, ServerProperties.Properties.NPC_MIN_RESPAWN_INTERVAL + 5);

				if (Name != Name.ToLower())
				{
					minutes += 5;
				}

				if (Level <= 65 && Realm == 0)
				{
					return minutes * 60000;
				}
				else if (Realm != 0)
				{
					// 5 to 10 minutes for realm npc's
					return Util.Random(5 * 60000, 10 * 60000);
				}
				else
				{
					int add = (Level - 65) + ServerProperties.Properties.NPC_MIN_RESPAWN_INTERVAL;
					return (minutes + add) * 60000;
				}
			}
			set
			{
				m_respawnInterval = value;
			}
		}

		/// <summary>
		/// True if NPC is alive, else false.
		/// </summary>
		public override bool IsAlive
		{
			get
			{
				bool alive = base.IsAlive;
				if (alive && IsRespawning)
					return false;
				return alive;
			}
		}

		/// <summary>
		/// True, if the mob is respawning, else false.
		/// </summary>
		public bool IsRespawning
		{
			get
			{
				if (m_respawnTimer == null)
					return false;
				return m_respawnTimer.IsAlive;
			}
		}

		/// <summary>
		/// Starts the Respawn Timer
		/// </summary>
		public virtual void StartRespawn()
		{
			if (IsAlive)
				return;

			if (m_healthRegenerationTimer != null)
			{
				m_healthRegenerationTimer.Stop();
				m_healthRegenerationTimer = null;
			}

			if (RespawnInterval <= 0)
				return;

			lock (_respawnTimerLock)
			{
				if (m_respawnTimer == null)
				{
					m_respawnTimer = new ECSGameTimer(this);
					m_respawnTimer.Callback = new ECSGameTimer.ECSTimerCallback(RespawnTimerCallback);
				}

				m_respawnTimer.Start(RespawnInterval);
			}
		}

		/// <summary>
		/// The callback that will respawn this mob
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
				}
			}

			if (IsAlive || ObjectState == eObjectState.Active)
				return 0;

			LoadTemplate(NPCTemplate);
			Health = MaxHealth;
			Mana = MaxMana;
			Endurance = MaxEndurance;
			m_x = m_spawnPoint.X;
			m_y = m_spawnPoint.Y;
			m_z = m_spawnPoint.Z;
			Heading = m_spawnHeading;
			SpawnTick = GameLoop.GameLoopTime;

			// Set stealth back on respawn instead of when the NPC is dying to prevent the corpse from immediately disappearing.
			if (WasStealthed)
				Flags |= eFlags.STEALTH;

			AddToWorld();
			return 0;
		}

		/// <summary>
		/// The chance for a critical hit
		/// </summary>
		public int AttackCriticalChance(DbInventoryItem weapon)
		{
			if (m_activeWeaponSlot == eActiveWeaponSlot.Distance)
			{
				if (rangeAttackComponent.RangedAttackType == eRangedAttackType.Critical)
					return 0; // no crit damage for crit shots
				else
					return GetModified(eProperty.CriticalArcheryHitChance);
			}

			return GetModified(eProperty.CriticalMeleeHitChance);
		}

		public override void OnAttackedByEnemy(AttackData ad)
		{
			if (ad.AttackType is AttackData.eAttackType.Spell && ad.Damage > 0 && Brain is IControlledBrain controlledBrain)
			{
				GamePlayer player = controlledBrain.GetPlayerOwner();

				if (player != null)
				{
					string modMessage = string.Empty;

					if (ad.Modifier > 0)
						modMessage = $" (+{ad.Modifier})";
					else if (ad.Modifier < 0)
						modMessage = $" ({ad.Modifier})";

					player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameLiving.AttackData.HitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.Damage, modMessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

					if (ad.CriticalDamage > 0)
						player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameLiving.AttackData.CriticallyHitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.CriticalDamage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
				}
			}

			if (Brain is StandardMobBrain standardMobBrain)
				standardMobBrain.OnAttackedByEnemy(ad);

			Flags &= ~eFlags.STEALTH;
			base.OnAttackedByEnemy(ad);
		}

		public virtual bool CanDropLoot => true;

		/// <summary>
		/// The enemy is healed, so we add to the xp gainers list
		/// </summary>
		/// <param name="enemy"></param>
		/// <param name="healSource"></param>
		/// <param name="changeType"></param>
		/// <param name="healAmount"></param>
		public override void EnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType, int healAmount)
		{
			base.EnemyHealed(enemy, healSource, changeType, healAmount);

			if (changeType != eHealthChangeType.Spell)
				return;
			if (enemy == healSource)
				return;
			if (!IsAlive)
				return;

			GameLiving healSourceLiving = healSource as GameLiving;

			if (healSourceLiving == null)
				return;

			Group attackerGroup = healSourceLiving.Group;
			if (attackerGroup != null)
			{
				// collect "helping" group players in range
				var xpGainers = attackerGroup.GetMembersInTheGroup()
					.Where(l => this.IsWithinRadius(l, WorldMgr.MAX_EXPFORKILL_DISTANCE) && l.IsAlive && l.ObjectState == eObjectState.Active).ToArray();

				float damageAmount = (float)healAmount / xpGainers.Length;

				foreach (GameLiving living in xpGainers)
				{
					// add players in range for exp to exp gainers
					this.AddXPGainer(living, damageAmount);
				}
			}
			else
			{
				this.AddXPGainer(healSourceLiving, healAmount);
			}

			if (healSource is GamePlayer || (healSource is GameNPC healSourceNpc && (healSourceNpc.Flags & eFlags.PEACE) == 0))
			{
				// first check to see if the healer is in our aggrolist so we don't go attacking anyone who heals
				if (Brain is StandardMobBrain mobBrain && mobBrain.GetBaseAggroAmount(healSourceLiving) > 0)
					mobBrain.AddToAggroList(healSourceLiving, healAmount);
			}

			//DealDamage needs to be called after addxpgainer!
		}

		public override long LastAttackTickPvE
		{
			set
			{
				base.LastAttackTickPvE = value;

				if (Brain is IControlledBrain controlledBrain)
					controlledBrain.Owner.LastAttackTickPvE = value;
			}
		}

		public override long LastAttackTickPvP
		{
			set
			{
				base.LastAttackTickPvP = value;

				if (Brain is IControlledBrain controlledBrain)
					controlledBrain.Owner.LastAttackTickPvP = value;
			}
		}

		public override long LastAttackedByEnemyTickPvE
		{
			set
			{
				base.LastAttackedByEnemyTickPvE = value;

				if (Brain is IControlledBrain controlledBrain)
					controlledBrain.Owner.LastAttackedByEnemyTickPvE = value;
			}
		}

		public override long LastAttackedByEnemyTickPvP
		{
			set
			{
				base.LastAttackedByEnemyTickPvP = value;

				if (Brain is IControlledBrain controlledBrain)
					controlledBrain.Owner.LastAttackedByEnemyTickPvP = value;
			}
		}

		#endregion

		#region Spell

		private List<Spell> m_spells = [];
		private ConcurrentDictionary<GameObject, List<SpellWaitingForLosCheck>> _spellsWaitingForLosCheck = new();

		public void ClearSpellsWaitingForLosCheck()
		{
			_spellsWaitingForLosCheck.Clear();
		}

		public class SpellWaitingForLosCheck
		{
			public Spell Spell { get; set; }
			public SpellLine SpellLine { get; set; }
			public long RequestTime { get; set; }

			public SpellWaitingForLosCheck(Spell spell, SpellLine spellLine, long requestTime)
			{
				Spell = spell;
				SpellLine = spellLine;
				RequestTime = requestTime;
			}
		}

		/// <summary>
		/// property of spell array of NPC
		/// </summary>
		public virtual List<Spell> Spells
		{
			get => m_spells;
			set
			{
				if (value == null || value.Count < 1)
				{
					m_spells.Clear();
					InstantHarmfulSpells = null;
					HarmfulSpells = null;
					InstantHealSpells = null;
					HealSpells = null;
					InstantMiscSpells = null;
					MiscSpells = null;
				}
				else
				{
					// Voluntary copy. This isn't ideal and needs to be changed eventually.
					m_spells = value.ToList();
					SortSpells();
				}
			}
		}

		public List<Spell> HarmfulSpells { get; set; } = null;
		public List<Spell> InstantHarmfulSpells { get; set; } = null;
		public List<Spell> HealSpells { get; set; } = null;
		public List<Spell> InstantHealSpells { get; set; } = null;
		public List<Spell> MiscSpells { get; set; } = null;
		public List<Spell> InstantMiscSpells { get; set; } = null;

		// These should only be used to check if the lists have something in them.
		public bool CanCastHarmfulSpells => HarmfulSpells != null && HarmfulSpells.Count > 0;
		public bool CanCastInstantHarmfulSpells => InstantHarmfulSpells != null && InstantHarmfulSpells.Count > 0;
		public bool CanCastHealSpells => HealSpells != null && HealSpells.Count > 0;
		public bool CanCastInstantHealSpells => InstantHealSpells != null && InstantHealSpells.Count > 0;
		public bool CanCastMiscSpells => MiscSpells != null && MiscSpells.Count > 0;
		public bool CanCastInstantMiscSpells => InstantMiscSpells != null && InstantMiscSpells.Count > 0;

		private long _nextInstantHarmfulSpell;
		public virtual bool IsInstantHarmfulSpellCastingLocked => !ServiceUtils.ShouldTick(_nextInstantHarmfulSpell);

		public virtual void ApplyInstantHarmfulSpellDelay()
		{
			// Delay the next spell by 1~6 seconds (triangular distribution).
			_nextInstantHarmfulSpell = GameLoop.GameLoopTime + 1000 + Util.Random(2500) + Util.Random(2500);
		}

		/// <summary>
		/// Sort spells into specific lists
		/// </summary>
		public virtual void SortSpells()
		{
			if (Spells.Count < 1)
				return;

			// Clear the lists
			if (InstantHarmfulSpells != null)
				InstantHarmfulSpells.Clear();
			if (HarmfulSpells != null)
				HarmfulSpells.Clear();

			if (InstantHealSpells != null)
				InstantHealSpells.Clear();
			if (HealSpells != null)
				HealSpells.Clear();

			if (InstantMiscSpells != null)
				InstantMiscSpells.Clear();
			if (MiscSpells != null)
				MiscSpells.Clear();

			// Sort spells into lists
			foreach (Spell spell in m_spells)
			{
				if (spell == null)
					continue;

				if (spell.IsHarmful)
				{
					if (spell.IsInstantCast)
					{
						if (InstantHarmfulSpells == null)
							InstantHarmfulSpells = new List<Spell>(1);
						InstantHarmfulSpells.Add(spell);
					}
					else
					{
						if (HarmfulSpells == null)
							HarmfulSpells = new List<Spell>(1);
						HarmfulSpells.Add(spell);
					}
				}
				else if (spell.IsHealing)
				{
					if (spell.IsInstantCast)
					{
						if (InstantHealSpells == null)
							InstantHealSpells = new List<Spell>(1);
						InstantHealSpells.Add(spell);
					}
					else
					{
						if (HealSpells == null)
							HealSpells = new List<Spell>(1);
						HealSpells.Add(spell);
					}
				}
				else
				{
					if (spell.IsInstantCast)
					{
						if (InstantMiscSpells == null)
							InstantMiscSpells = new List<Spell>(1);
						InstantMiscSpells.Add(spell);
					}
					else
					{
						if (MiscSpells == null)
							MiscSpells = new List<Spell>(1);
						MiscSpells.Add(spell);
					}
				}
			}
		}

		public virtual void ScaleSpell(Spell spell, int casterLevel, double baseLineLevel)
		{
			if (spell == null || Level < 1 || spell.ScaledToNpcLevel)
				return;

			if (casterLevel < 1)
				casterLevel = Level;

			double scalingFactor = casterLevel / baseLineLevel;

			switch (spell.SpellType)
			{
				// Scale Damage.
				case eSpellType.DamageOverTime:
				case eSpellType.DamageShield:
				case eSpellType.DamageAdd:
				case eSpellType.DirectDamage:
				case eSpellType.Lifedrain:
				case eSpellType.DamageSpeedDecrease:
				case eSpellType.StyleBleeding:
				{
					spell.Damage *= scalingFactor;
					spell.ScaledToNpcLevel = true;
					break;
				}
				// Scale Value.
				case eSpellType.EnduranceRegenBuff:
				case eSpellType.Heal:
				case eSpellType.StormEnduDrain:
				case eSpellType.PowerRegenBuff:
				case eSpellType.PowerHealthEnduranceRegenBuff:
				case eSpellType.CombatSpeedBuff:
				case eSpellType.HasteBuff:
				case eSpellType.CelerityBuff:
				case eSpellType.CombatSpeedDebuff:
				case eSpellType.StyleCombatSpeedDebuff:
				case eSpellType.CombatHeal:
				case eSpellType.HealthRegenBuff:
				case eSpellType.HealOverTime:
				case eSpellType.ConstitutionBuff:
				case eSpellType.DexterityBuff:
				case eSpellType.StrengthBuff:
				case eSpellType.ConstitutionDebuff:
				case eSpellType.DexterityDebuff:
				case eSpellType.StrengthDebuff:
				case eSpellType.ArmorFactorDebuff:
				case eSpellType.BaseArmorFactorBuff:
				case eSpellType.SpecArmorFactorBuff:
				case eSpellType.PaladinArmorFactorBuff:
				case eSpellType.ArmorAbsorptionBuff:
				case eSpellType.ArmorAbsorptionDebuff:
				case eSpellType.DexterityQuicknessBuff:
				case eSpellType.StrengthConstitutionBuff:
				case eSpellType.DexterityQuicknessDebuff:
				case eSpellType.StrengthConstitutionDebuff:
				case eSpellType.Taunt:
				case eSpellType.SpeedDecrease:
				case eSpellType.SavageCombatSpeedBuff:
				{
					spell.Value *= scalingFactor;
					spell.ScaledToNpcLevel = true;
					break;
				}
				// Scale Duration.
				case eSpellType.Disease:
				case eSpellType.Stun:
				case eSpellType.UnrresistableNonImunityStun:
				case eSpellType.Mesmerize:
				case eSpellType.StyleStun:
				case eSpellType.StyleSpeedDecrease:
				{
					spell.Duration = (int) Math.Ceiling(spell.Duration * scalingFactor);
					spell.ScaledToNpcLevel = true;
					break;
				}
				// Scale Damage and Value.
				case eSpellType.DirectDamageWithDebuff:
				{
					// Patch 1.123: For Cabalist, Enchanter, and Spiritmaster pets
					// The debuff component of its nuke has been as follows:
					// For pet level 1-23, the debuff is now 10%.
					// For pet level 24-43, the debuff is now 20%.
					// For pet level 44-50, the debuff is now 30%.
					spell.Value *= scalingFactor;
					spell.Damage *= scalingFactor;
					spell.Duration = (int) Math.Ceiling(spell.Duration * scalingFactor);
					spell.ScaledToNpcLevel = true;
					break;
				}
				case eSpellType.StyleTaunt:
				case eSpellType.CurePoison:
				case eSpellType.CureDisease:
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Cast a spell, with optional LOS check
		/// </summary>
		public virtual bool CastSpell(Spell spell, SpellLine line, bool checkLos)
		{
			bool casted;

			if (checkLos)
				casted = CastSpell(spell, line);
			else
			{
				Spell spellToCast;

				if (line.KeyName == GlobalSpellsLines.Mob_Spells)
				{
					// NPC spells will get the level equal to their caster
					spellToCast = (Spell)spell.Clone();
					spellToCast.Level = Level;
				}
				else
					spellToCast = spell;

				casted = base.CastSpell(spellToCast, line);
			}

			return casted;
		}

		/// <summary>
		/// Cast a spell with LoS check if possible.
		/// </summary>
		/// <returns>True if the spellcast started successfully. False otherwise or if a LoS check was initiated.</returns>
		public override bool CastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null)
		{
			// Clean up our '_spellsWaitingForLosCheck'. Entries older than 2 seconds are removed.
			foreach (var pair in _spellsWaitingForLosCheck)
			{
				List<SpellWaitingForLosCheck> list = pair.Value;

				lock (((ICollection) list).SyncRoot)
				{
					for (int i = list.Count - 1; i >= 0; i--)
					{
						if (ServiceUtils.ShouldTick(list[i].RequestTime + 2000))
							list.SwapRemoveAt(i);
					}

					// We can keep the list if we're about to add anything to it.
					if (list.Count == 0 && TargetObject != pair.Key)
						_spellsWaitingForLosCheck.TryRemove(pair.Key, out _);
				}
			}

			Spell spellToCast = null;

			if (line.KeyName is GlobalSpellsLines.Mob_Spells)
			{
				// NPC spells will get the level equal to their caster
				spellToCast = (Spell) spell.Clone();
				spellToCast.Level = Level;
			}
			else
				spellToCast = spell;

			if (TargetObject == this || TargetObject == null)
				return base.CastSpell(spellToCast, line);

			GamePlayer LosChecker = TargetObject as GamePlayer;

			if (LosChecker == null && Brain is IControlledBrain controlledBrain)
				LosChecker = controlledBrain.GetPlayerOwner();

			if (LosChecker == null && Brain is StandardMobBrain brain)
			{
				List<GamePlayer> playersInRadius = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);

				if (playersInRadius.Count > 0)
					LosChecker = playersInRadius[Util.Random(playersInRadius.Count - 1)];
			}

			if (LosChecker == null)
				return base.CastSpell(spellToCast, line, spellCastingAbilityHandler);

			_spellsWaitingForLosCheck.AddOrUpdate(TargetObject, Add, Update, new SpellWaitingForLosCheck(spellToCast, line, GameLoop.GameLoopTime));
			return false;

			List<SpellWaitingForLosCheck> Add(GameObject key, SpellWaitingForLosCheck arg)
			{
				LosChecker.Out.SendCheckLos(this, TargetObject, new CheckLosResponse(CastSpellLosCheckReply));
				List<SpellWaitingForLosCheck> list = [arg];
				return list;
			}

			List<SpellWaitingForLosCheck> Update(GameObject key, List<SpellWaitingForLosCheck> oldValue, SpellWaitingForLosCheck arg)
			{
				// This LoS check will not necessarily result in an actual packet being sent to the client, but it will trigger a second call to the callback.
				LosChecker.Out.SendCheckLos(this, TargetObject, new CheckLosResponse(CastSpellLosCheckReply));
				oldValue.Add(arg);
				return oldValue;
			}
		}

		public virtual void CastSpellLosCheckReply(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			GameObject target = CurrentRegion.GetObject(targetOID);

			if (target == null)
				return;

			if (!_spellsWaitingForLosCheck.TryRemove(target, out List<SpellWaitingForLosCheck> list))
				return;

			bool success = response is eLosCheckResponse.TRUE;
			List<SpellWaitingForLosCheck> spellsWaitingForLosCheck;

			lock (((ICollection) list).SyncRoot)
			{
				spellsWaitingForLosCheck = list.ToList();
				// Don't bother removing the list here. It'll be done by `CastSpell`.
				list.Clear();
			}

			foreach (SpellWaitingForLosCheck spellWaitingForLosCheck in spellsWaitingForLosCheck)
			{
				Spell spell = spellWaitingForLosCheck.Spell;
				SpellLine spellLine = spellWaitingForLosCheck.SpellLine;

				if (success && spellLine != null && spell != null)
					OnCastSpellLosCheckSuccess(target, spell, spellLine);
				else
					OnCastSpellLosCheckFail(target);
			}
		}

		public virtual void OnCastSpellLosCheckSuccess(GameObject target, Spell spell, SpellLine spellLine)
		{
			CastSpell(spell, spellLine, target as GameLiving);
		}

		public virtual void OnCastSpellLosCheckFail(GameObject target)
		{
			// In case the NPC changes target while casting on the current one and the first LoS check was positive.
			if (castingComponent.QueuedSpellHandler?.Target == target)
				castingComponent.ClearUpQueuedSpellHandler();
		}

		#endregion

		#region Styles

		/// <summary>
		/// Styles for this NPC
		/// </summary>
		private List<Style> m_styles = [];
		public List<Style> Styles
		{
			get => m_styles;
			set
			{
				m_styles = value;
				SortStyles();
			}
		}

		/// <summary>
		/// Chain styles for this NPC
		/// </summary>
		public List<Style> StylesChain { get; protected set; } = null;

		/// <summary>
		/// Defensive styles for this NPC
		/// </summary>
		public List<Style> StylesDefensive { get; protected set; } = null;

		/// <summary>
		/// Back positional styles for this NPC
		/// </summary>
		public List<Style> StylesBack { get; protected set; } = null;

		/// <summary>
		/// Side positional styles for this NPC
		/// </summary>
		public List<Style> StylesSide { get; protected set; } = null;

		/// <summary>
		/// Front positional styles for this NPC
		/// </summary>
		public List<Style> StylesFront { get; protected set; } = null;

		/// <summary>
		/// Anytime styles for this NPC
		/// </summary>
		public List<Style> StylesAnytime { get; protected set; } = null;

		/// <summary>
		/// Sorts styles by type for more efficient style selection later
		/// </summary>
		public virtual void SortStyles()
		{
			if (StylesChain != null)
				StylesChain.Clear();

			if (StylesDefensive != null)
				StylesDefensive.Clear();

			if (StylesBack != null)
				StylesBack.Clear();

			if (StylesSide != null)
				StylesSide.Clear();

			if (StylesFront != null)
				StylesFront.Clear();

			if (StylesAnytime != null)
				StylesAnytime.Clear();

			if (m_styles == null)
				return;

			foreach (Style s in m_styles)
			{
				if (s == null)
				{
					if (log.IsWarnEnabled)
					{
						String sError = $"GameNPC.SortStyles(): NULL style for NPC named {Name}";
						if (m_InternalID != null)
							sError += $", InternalID {this.m_InternalID}";
						if (m_npcTemplate != null)
							sError += $", NPCTemplateID {m_npcTemplate.TemplateId}";
						log.Warn(sError);
					}
					continue; // Keep sorting, as a later style may not be null
				}// if (s == null)

				switch (s.OpeningRequirementType)
				{
					case Style.eOpening.Defensive:
						if (StylesDefensive == null)
							StylesDefensive = new List<Style>(1);
						StylesDefensive.Add(s);
						break;
					case Style.eOpening.Positional:
						switch ((Style.eOpeningPosition)s.OpeningRequirementValue)
						{
							case Style.eOpeningPosition.Back:
								if (StylesBack == null)
									StylesBack = new List<Style>(1);
								StylesBack.Add(s);
								break;
							case Style.eOpeningPosition.Side:
								if (StylesSide == null)
									StylesSide = new List<Style>(1);
								StylesSide.Add(s);
								break;
							case Style.eOpeningPosition.Front:
								if (StylesFront == null)
									StylesFront = new List<Style>(1);
								StylesFront.Add(s);
								break;
							default:
								if (log.IsWarnEnabled)
									log.Warn($"GameNPC.SortStyles(): Invalid OpeningRequirementValue for positional style {s.Name }, ID {s.ID}, ClassId {s.ClassID}");

								break;
						}
						break;
					default:
						if (s.OpeningRequirementValue > 0)
						{
							if (StylesChain == null)
								StylesChain = new List<Style>(1);
							StylesChain.Add(s);
						}
						else
						{
							if (StylesAnytime == null)
								StylesAnytime = new List<Style>(1);
							StylesAnytime.Add(s);
						}
						break;
				}// switch (s.OpeningRequirementType)
			}// foreach
		}// SortStyles()

		/// <summary>
		/// Can we use this style without spamming a stun style?
		/// </summary>
		/// <param name="style">The style to check.</param>
		/// <returns>True if we should use the style, false if it would be spamming a stun effect.</returns>
		public bool CheckStyleStun(Style style)
		{
			if (TargetObject is GameLiving living && style.Procs.Count > 0)
			{
				foreach (StyleProcInfo t in style.Procs)
				{
					if (t.Spell.SpellType == eSpellType.StyleStun && living.HasEffect(t.Spell))
						return false;
				}
			}

			return true;
		}

		///// <summary>
		///// Picks a style, prioritizing reactives an	d chains over positionals and anytimes
		///// </summary>
		///// <returns>Selected style</returns>
		//public override Style GetStyleToUse()
		//{
		//	if (m_styles == null || m_styles.Count < 1 || TargetObject == null)
		//		return null;

		//	// Chain and defensive styles skip the GAMENPC_CHANCES_TO_STYLE,
		//	//	or they almost never happen e.g. NPC blocks 10% of the time,
		//	//	default 20% style chance means the defensive style only happens
		//	//	2% of the time, and a chain from it only happens 0.4% of the time.
		//	if (StylesChain != null && StylesChain.Count > 0)
		//		foreach (Style s in StylesChain)
		//			if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
		//				return s;

		//	if (StylesDefensive != null && StylesDefensive.Count > 0)
		//		foreach (Style s in StylesDefensive)
		//			if (StyleProcessor.CanUseStyle(this, s, AttackWeapon)
		//				&& CheckStyleStun(s)) // Make sure we don't spam stun styles like Brutalize
		//				return s;

		//	if (Util.Chance(Properties.GAMENPC_CHANCES_TO_STYLE))
		//	{
		//		// Check positional styles
		//		// Picking random styles allows mobs to use multiple styles from the same position
		//		//	e.g. a mob with both Pincer and Ice Storm side styles will use both of them.
		//		if (StylesBack != null && StylesBack.Count > 0)
		//		{
		//			Style s = StylesBack[Util.Random(0, StylesBack.Count - 1)];
		//			if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
		//				return s;
		//		}

		//		if (StylesSide != null && StylesSide.Count > 0)
		//		{
		//			Style s = StylesSide[Util.Random(0, StylesSide.Count - 1)];
		//			if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
		//				return s;
		//		}

		//		if (StylesFront != null && StylesFront.Count > 0)
		//		{
		//			Style s = StylesFront[Util.Random(0, StylesFront.Count - 1)];
		//			if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
		//				return s;
		//		}

		//		// Pick a random anytime style
		//		if (StylesAnytime != null && StylesAnytime.Count > 0)
		//			return StylesAnytime[Util.Random(0, StylesAnytime.Count - 1)];
		//	}

		//	return null;
		//} // GetStyleToUse()

		/// <summary>
		/// The Abilities for this NPC
		/// </summary>
		public Dictionary<string, Ability> Abilities
		{
			get
			{
				Dictionary<string, Ability> tmp = new Dictionary<string, Ability>();

				lock (_abilitiesLock)
				{
					tmp = new Dictionary<string, Ability>(m_abilities);
				}

				return tmp;
			}
			protected set => m_abilities = value;
		}

		#endregion

		#region Notify

		/// <summary>
		/// Handle event notifications
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="sender">The sender</param>
		/// <param name="args">The arguements</param>
		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			base.Notify(e, sender, args);

			ABrain brain = Brain;
			if (brain != null)
				brain.Notify(e, sender, args);

		}

		/// <summary>
		/// Handles all ambient messages triggered by a mob or NPC action
		/// </summary>
		/// <param name="trigger">The action triggering the message (e.g., aggroing, dying, roaming)</param>
		/// <param name="living">The entity triggering the action (e.g., a player)</param>
		public virtual void FireAmbientSentence(eAmbientTrigger trigger, GameObject living)
		{
			if (IsSilent || ambientTexts == null || ambientTexts.Count == 0) return;
			if (trigger == eAmbientTrigger.interact && living == null) return; // Do not trigger interact messages with a corpse
			List<DbMobXAmbientBehavior> mxa = (from i in ambientTexts where i.Trigger == trigger.ToString() select i).ToList();
			if (mxa.Count == 0) return;

			// grab random sentence
			var chosen = mxa[Util.Random(mxa.Count - 1)];
			if (!Util.Chance(chosen.Chance)) return;

			string controller = string.Empty;
			if (Brain is IControlledBrain) // Used for '{controller}' trigger keyword, use the name of the mob's owner (else returns blank)--this is used when a pet has an ambient trigger.
			{
				GamePlayer playerOwner = ((IControlledBrain) Brain).GetPlayerOwner();
				if (playerOwner != null)
					controller = playerOwner.Name;
			}

			string text = chosen.Text;

			if (TargetObject == null)
			{
				text = chosen.Text.Replace("{sourcename}", Brain?.Body?.Name) // '{sourcename}' returns the mob or NPC name
					.Replace("{targetname}", living?.Name) // '{targetname}' returns the mob/NPC target's name
					.Replace("{controller}", controller); // '{controller}' returns the result of the controller var (use this when pets have dialogue)
				
				// Replace trigger keywords
				if (living is GamePlayer)
					text = text.Replace("{class}", ((GamePlayer) living).CharacterClass.Name).Replace("{race}", ((GamePlayer) living).RaceName);
				if (living is GameNPC)
					text = text.Replace("{class}", "NPC").Replace("{race}", "NPC");
			}
			else
			{
				text = chosen.Text.Replace("{sourcename}", Brain.Body.Name) // '{sourcename}' returns the mob or NPC name
					.Replace("{targetname}", TargetObject == null ? string.Empty : TargetObject.Name) // '{targetname}' returns the mob/NPC target's name
					.Replace("{controller}", controller); // '{controller}' returns the result of the controller var (use this when pets have dialogue)
				
				// Replace trigger keywords
				if (TargetObject is GamePlayer)
					text = text.Replace("{class}", ((GamePlayer) TargetObject).CharacterClass.Name).Replace("{race}", ((GamePlayer) TargetObject).RaceName);
				if (TargetObject is GameNPC)
					text = text.Replace("{class}", "NPC").Replace("{race}", "NPC");
			}
			// Replace trigger keywords

			if (chosen.Emote != 0)
			{
				Emote((eEmote)chosen.Emote);
			}
			
			// Replace trigger keywords
			if (TargetObject is GamePlayer && living is GamePlayer)
				text = text.Replace("{class}", ((GamePlayer) living).CharacterClass.Name).Replace("{race}", ((GamePlayer) living).RaceName);
			if (TargetObject is GameNPC && living is GameNPC)
				text = text.Replace("{class}", "NPC").Replace("{race}", "NPC");
			
			/*// Determines message delivery method for trigger voice
			if (chosen.Voice.StartsWith("b")) // Broadcast message without "[Broadcast] {0}:" string start
			{
				foreach (GamePlayer player in CurrentRegion.GetPlayersInRadius(X, Y, Z, 25000, false, false))
				{
					player.Out.SendMessage(text, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
				}
				return;
			}
			if (chosen.Voice.StartsWith("y")) // Yell message (increased range) without "{0} yells," string start
			{
				Yell(text);
				return;
			}*/
			
			// Determines message delivery method for triggers
			switch (chosen.Voice)
			{
				case "b": // Broadcast message without "[Broadcast] {0}:" string start
				{
					foreach (GamePlayer player in GetPlayersInRadius(25000))
					{
					  player.Out.SendMessage(text, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
					}
					return;
				}
				case "y": // Yell message (increased range) without "{0} yells," string start
				{
					Yell(text);
					return;
				}
				case "s": // Return custom System message in System/Combat window to all players within range
				{
					Message.MessageToArea(Brain.Body, text, eChatType.CT_System, eChatLoc.CL_SystemWindow, 512, null);
					return;
				}
				case "c": // Return custom Say message in Chat window to all players within range, without "{0} says," string start
				{
					Message.MessageToArea(Brain.Body, text, eChatType.CT_Say, eChatLoc.CL_ChatWindow, 512, null);
					return;
				}
				case "p": // Return custom System message in popup dialog only to player interating with the NPC
					// For interact triggers
				{
					((GamePlayer) living).Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					return;
				}
				default: // Return Say message with "{0} says," string start included (contrary to parameter description)
				{
					Say(text);
					return;
				}
			}
		}
		#endregion

		#region ControlledNPCs

		public override bool AddControlledBrain(IControlledBrain controlledBrain)
		{
			ControlledBrain = controlledBrain;
			return true;
		}

		public override bool RemoveControlledBrain(IControlledBrain controlledBrain)
		{
			if (ControlledBrain == controlledBrain)
			{
				InitControlledBrainArray(1);
				ControlledBrain = null;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the controlled object of this NPC
		/// </summary>
		public override IControlledBrain ControlledBrain
		{
			get
			{
				if (m_controlledBrain == null) return null;
				return m_controlledBrain[0];
			}
		}

		/// <summary>
		/// Gets the controlled array of this NPC
		/// </summary>
		public IControlledBrain[] ControlledNpcList
		{
			get { return m_controlledBrain; }
		}

		#endregion

		/// <summary>
		/// Whether this NPC is available to add on a fight.
		/// </summary>
		public virtual bool IsAvailableToJoinFight => !InCombat && Brain is not IControlledBrain && Brain is StandardMobBrain brain && !brain.HasAggro;

		/// <summary>
		/// Whether this NPC is aggressive.
		/// </summary>
		public virtual bool IsAggressive => Brain is IOldAggressiveBrain;

		/// <summary>
		/// Whether this NPC is a friend or not.
		/// </summary>
		/// <param name="npc">The NPC that is checked against.</param>
		/// <returns></returns>
		public virtual bool IsFriend(GameNPC npc)
		{
			if (Faction == null || npc.Faction == null)
				return false;

			return Faction.FriendFactions.Contains(npc.Faction);
		}

		/// <summary>
		/// Broadcast loot to the raid.
		/// </summary>
		/// <param name="dropMessages">List of drop messages to broadcast.</param>
		protected virtual void BroadcastLoot(ArrayList droplist)
		{
			if (droplist.Count > 0)
			{
				String lastloot;
				foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
				{
					lastloot = string.Empty;
					foreach (string str in droplist)
					{
						// Suppress identical messages (multiple item drops).
						if (str != lastloot)
						{
							player.Out.SendMessage(String.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.DropLoot.Drops",
								GetName(0, true, player.Client.Account.Language, this), str)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
							lastloot = str;
						}
					}
				}
			}
		}

		public override eGender Gender { get; set; }

		public new NpcCastingComponent castingComponent;
		public new NpcMovementComponent movementComponent;

		public GameNPC Copy()
		{
			return Copy(null);
		}

		/// <summary>
		/// Create a copy of the GameNPC
		/// </summary>
		/// <param name="copyTarget">A GameNPC to copy this GameNPC to (can be null)</param>
		/// <returns>The GameNPC this GameNPC was copied to</returns>
		public GameNPC Copy(GameNPC copyTarget)
		{
			if (copyTarget == null)
				copyTarget = new GameNPC();

			copyTarget.TranslationId = TranslationId;
			copyTarget.BlockChance = BlockChance;
			copyTarget.BodyType = BodyType;
			copyTarget.CanUseLefthandedWeapon = CanUseLefthandedWeapon;
			copyTarget.Charisma = Charisma;
			copyTarget.Constitution = Constitution;
			copyTarget.CurrentRegion = CurrentRegion;
			copyTarget.Dexterity = Dexterity;
			copyTarget.Empathy = Empathy;
			copyTarget.Endurance = Endurance;
			copyTarget.EquipmentTemplateID = EquipmentTemplateID;
			copyTarget.EvadeChance = EvadeChance;
			copyTarget.Faction = Faction;
			copyTarget.Flags = Flags;
			copyTarget.GuildName = GuildName;
			copyTarget.ExamineArticle = ExamineArticle;
			copyTarget.MessageArticle = MessageArticle;
			copyTarget.Heading = Heading;
			copyTarget.Intelligence = Intelligence;
			copyTarget.IsCloakHoodUp = IsCloakHoodUp;
			copyTarget.IsCloakInvisible = IsCloakInvisible;
			copyTarget.IsHelmInvisible = IsHelmInvisible;
			copyTarget.LeftHandSwingChance = LeftHandSwingChance;
			copyTarget.Level = Level;
			copyTarget.LoadedFromScript = LoadedFromScript;
			copyTarget.MaxSpeedBase = MaxSpeedBase;
			copyTarget.MeleeDamageType = MeleeDamageType;
			copyTarget.Model = Model;
			copyTarget.Name = Name;
			copyTarget.Suffix = Suffix;
			copyTarget.NPCTemplate = NPCTemplate;
			copyTarget.ParryChance = ParryChance;
			copyTarget.PathID = PathID;
			copyTarget.Quickness = Quickness;
			copyTarget.Piety = Piety;
			copyTarget.Race = Race;
			copyTarget.Realm = Realm;
			copyTarget.RespawnInterval = RespawnInterval;
			copyTarget.RoamingRange = RoamingRange;
			copyTarget.Size = Size;
			copyTarget.SaveInDB = SaveInDB;
			copyTarget.Strength = Strength;
			copyTarget.TetherRange = TetherRange;
			copyTarget.X = X;
			copyTarget.Y = Y;
			copyTarget.Z = Z;
			copyTarget.OwnerID = OwnerID;
			copyTarget.PackageID = PackageID;

			if (Abilities != null && Abilities.Count > 0)
			{
				foreach (Ability targetAbility in Abilities.Values)
				{
					if (targetAbility != null)
						copyTarget.AddAbility(targetAbility);
				}
			}

			ABrain brain = null;
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				brain = (ABrain)assembly.CreateInstance(Brain.GetType().FullName, true);
				if (brain != null)
					break;
			}

			if (brain == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("GameNPC.Copy():  Unable to create brain:  " + Brain.GetType().FullName + ", using StandardMobBrain.");

				brain = new StandardMobBrain();
			}

			StandardMobBrain newBrainSMB = brain as StandardMobBrain;
			StandardMobBrain thisBrainSMB = this.Brain as StandardMobBrain;

			if (newBrainSMB != null && thisBrainSMB != null)
			{
				newBrainSMB.AggroLevel = thisBrainSMB.AggroLevel;
				newBrainSMB.AggroRange = thisBrainSMB.AggroRange;
			}

			copyTarget.SetOwnBrain(brain);

			if (Inventory != null && Inventory.AllItems.Count > 0)
			{
				GameNpcInventoryTemplate inventoryTemplate = Inventory as GameNpcInventoryTemplate;

				if (inventoryTemplate != null)
					copyTarget.Inventory = inventoryTemplate.CloneTemplate();
			}

			if (Spells != null && Spells.Count > 0)
				copyTarget.Spells = new List<Spell>(Spells);

			if (Styles != null && Styles.Count > 0)
				copyTarget.Styles = new List<Style>(Styles);

			if (copyTarget.Inventory != null)
				copyTarget.SwitchWeapon(ActiveWeaponSlot);

			return copyTarget;
		}

		public GameNPC(ABrain defaultBrain) : base()
		{
			castingComponent ??= base.castingComponent as NpcCastingComponent;
			movementComponent ??= base.movementComponent as NpcMovementComponent;

			Level = 1;
			m_health = MaxHealth;
			Realm = 0;
			m_name = "new mob";
			m_model = 408;
			MaxSpeedBase = 200;
			GuildName = string.Empty;
			m_size = 50;
			m_flags = 0;
			RoamingRange = 0;
			OwnerID = string.Empty;
			m_spawnPoint = new Point3D();
			LinkedFactions = new ArrayList(1);

			if (m_ownBrain == null)
			{
				defaultBrain.Body = this;
				SetOwnBrain(defaultBrain);
			}
		}

		public GameNPC() : this(new StandardMobBrain()) { }

		public GameNPC(INpcTemplate template) : this()
		{
			if (template == null)
				return;

			if (template is NpcTemplate npcTemplate)
				npcTemplate.ReplaceMobValues = true;

			LoadTemplate(template);
		}

		private double m_campBonus = 1;

		public virtual bool CanAwardKillCredit => false;
		public virtual double CampBonus { get => m_campBonus; set => m_campBonus = value; }
		public virtual double MaxHealthScalingFactor => 1.0;
		public double DamageFactor { get => damageFactor; set => damageFactor = value; }
		public int OrbsReward { get => orbsReward; set => orbsReward = value; }
	}
}
