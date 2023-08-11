/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Housing;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.GS.ServerProperties;
using DOL.GS.Styles;
using DOL.GS.Utils;
using DOL.Language;
using ECS.Debug;

namespace DOL.GS
{
	/// <summary>
	/// This class is the baseclass for all Non Player Characters like
	/// Monsters, Merchants, Guards, Steeds ...
	/// </summary>
	public class GameNPC : GameLiving, ITranslatableObject
	{
		public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private const int VISIBLE_TO_PLAYER_SPAN = 60000;

		private int m_databaseLevel;

		public override eGameObjectType GameObjectType => eGameObjectType.NPC;
		public bool NeedsBroadcastUpdate { get; set; }

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
		protected string m_translationId = "";

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

				ushort oldHeading = base.Heading;
				base.Heading = value;

				if (base.Heading != oldHeading)
					NeedsBroadcastUpdate = true;
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
				bool bMaxHealth = (m_health == MaxHealth);

				if (Level != value)
				{
					if (Level < 1 && ObjectState == eObjectState.Active)
					{
						// This is a newly created NPC, so notify nearby players of its creation
						foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							player.Out.SendNPCCreate(this);
							if (m_inventory != null)
								player.Out.SendLivingEquipmentUpdate(this);
						}
					}

					base.Level = value;
					AutoSetStats();  // Recalculate stats when level changes
				}
				else
					base.Level = value;

				if (bMaxHealth)
					m_health = MaxHealth;
			}
		}

		/// <summary>
		/// Auto set stats based on DB entry, npcTemplate, and level.
		/// </summary>
		/// <param name="dbMob">Mob DB entry to load stats from, retrieved from DB if null</param>
		public virtual void AutoSetStats(Mob dbMob = null)
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
				Charisma = NPCTemplate.Strength;
			}
			else
			{
				Mob mob = dbMob;

				if (mob == null && !string.IsNullOrEmpty(InternalID))
					// This should only happen when a GM command changes level on a mob with no npcTemplate,
					mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);

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
				else
				{
					if (Level > 1)
					{
						int levelMinusOne = Level - 1;
						Strength = (short) (Properties.MOB_AUTOSET_STR_BASE + levelMinusOne * Properties.MOB_AUTOSET_STR_MULTIPLIER);
						Constitution = (short) (Properties.MOB_AUTOSET_CON_BASE + levelMinusOne * Properties.MOB_AUTOSET_CON_MULTIPLIER);
						Quickness = (short) (Properties.MOB_AUTOSET_QUI_BASE + levelMinusOne * Properties.MOB_AUTOSET_QUI_MULTIPLIER);
						Dexterity = (short) (Properties.MOB_AUTOSET_DEX_BASE + levelMinusOne * Properties.MOB_AUTOSET_DEX_MULTIPLIER);
						Intelligence = (short) (Properties.MOB_AUTOSET_INT_BASE + levelMinusOne * Properties.MOB_AUTOSET_INT_MULTIPLIER);
					}
				}
			}
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
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						player.Out.SendNPCCreate(this);
						if (m_inventory != null)
							player.Out.SendLivingEquipmentUpdate(this);
					}
				}
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
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						player.Out.SendNPCCreate(this);
						if (m_inventory != null)
							player.Out.SendLivingEquipmentUpdate(this);
					}
				}
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
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						player.Out.SendNPCCreate(this);
						if (m_inventory != null)
							player.Out.SendLivingEquipmentUpdate(this);
					}
				}
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
					{
						foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							player.Out.SendNPCCreate(this);
							if (m_inventory != null)
								player.Out.SendLivingEquipmentUpdate(this);
						}
					}
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

				if (IsTargetPositionValid)
				{
					long expectedDistance = FastMath.Abs((long) TargetPosition.X - m_x);

					if (expectedDistance == 0)
						return TargetPosition.X;

					long actualDistance = FastMath.Abs((long) (MovementElapsedTicks * movementComponent.TickSpeedX));

					if (expectedDistance - actualDistance < 0)
						return TargetPosition.X;
				}

				return (int) (m_x + MovementElapsedTicks * movementComponent.TickSpeedX);
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

				if (IsTargetPositionValid)
				{
					long expectedDistance = FastMath.Abs((long) TargetPosition.Y - m_y);

					if (expectedDistance == 0)
						return TargetPosition.Y;

					long actualDistance = FastMath.Abs((long) (MovementElapsedTicks * movementComponent.TickSpeedY));

					if (expectedDistance - actualDistance < 0)
						return TargetPosition.Y;
				}

				return (int) (m_y + MovementElapsedTicks * movementComponent.TickSpeedY);
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

				if (IsTargetPositionValid)
				{
					long expectedDistance = FastMath.Abs(TargetPosition.Z - m_z);

					if (expectedDistance == 0)
						return TargetPosition.Z;

					long actualDistance = FastMath.Abs((long) (MovementElapsedTicks * movementComponent.TickSpeedZ));

					if (expectedDistance - actualDistance < 0)
						return TargetPosition.Z;
				}

				return (int) (m_z + MovementElapsedTicks * movementComponent.TickSpeedZ);
			}
		}

		public int RealZ => m_z;

		/// <summary>
		/// The stealth state of this NPC
		/// </summary>
		public override bool IsStealthed => false;// (Flags & eFlags.STEALTH) != 0;
		public bool WasStealthed { get; private set; } = false;

		protected int m_maxdistance;
		/// <summary>
		/// The Mob's max distance from its spawn before return automatically
		/// if MaxDistance > 0 ... the amount is the normal value
		/// if MaxDistance = 0 ... no maxdistance check
		/// if MaxDistance less than 0 ... the amount is calculated in percent of the value and the aggrorange (in StandardMobBrain)
		/// </summary>
		public int MaxDistance
		{
			get { return m_maxdistance; }
			set { m_maxdistance = value; }
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

		public const int STICK_MINIMUM_RANGE = 75;
		public const int STICK_MAXIMUM_RANGE = 5000;

		public long LastVisibleToPlayersTickCount => m_lastVisibleToPlayerTick;

		public IPoint3D TargetPosition => movementComponent.TargetPosition;
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
		public bool IsTargetPositionValid => movementComponent.IsTargetPositionValid;
		public bool IsAtTargetPosition => movementComponent.IsAtTargetPosition;
		public bool CanRoam => movementComponent.CanRoam;

		public virtual void WalkTo(IPoint3D target, short speed)
		{
			movementComponent.WalkTo(target, speed);
		}

		public virtual void PathTo(IPoint3D target, short speed)
		{
			movementComponent.PathTo(target, speed);
		}

		public virtual void StopMoving()
		{
			movementComponent.StopMoving();
		}

		public virtual void Follow(GameObject target, int minDistance, int maxDistance)
		{
			movementComponent.Follow(target, minDistance, maxDistance);
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
			EquipmentTemplateID = equipmentTemplateID;
			if (EquipmentTemplateID != null && EquipmentTemplateID.Length > 0)
			{
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				if (template.LoadFromDatabase(EquipmentTemplateID))
				{
					m_inventory = template.CloseTemplate();
				}
				else
				{
					//if (log.IsDebugEnabled)
					//{
					//    //log.Warn("Error loading NPC inventory: InventoryID="+EquipmentTemplateID+", NPC name="+Name+".");
					//}
				}
				if (Inventory != null)
				{
					//if the distance slot isnt empty we use that
					if (Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
						SwitchWeapon(eActiveWeaponSlot.Distance);
					else
					{
						InventoryItem twohand = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
						InventoryItem onehand = Inventory.GetItem(eInventorySlot.RightHandWeapon);

						if (twohand != null && onehand != null)
							//Let's add some random chance
							SwitchWeapon(Util.Chance(50) ? eActiveWeaponSlot.TwoHanded : eActiveWeaponSlot.Standard);
						else if (twohand != null)
							//Hmm our right hand weapon may have been null
							SwitchWeapon(eActiveWeaponSlot.TwoHanded);
						else if (onehand != null)
							//Hmm twohand was null lets default down here
							SwitchWeapon(eActiveWeaponSlot.Standard);
					}
				}
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
			if (obj is not Mob)
				return;

			base.LoadFromDatabase(obj);

			m_loadedFromScript = false;
			Mob dbMob = (Mob)obj;
			NPCTemplate = NpcTemplateMgr.GetTemplate(dbMob.NPCTemplateID);
			TranslationId = dbMob.TranslationId;
			Name = dbMob.Name;
			Suffix = dbMob.Suffix;
			GuildName = dbMob.Guild;
			ExamineArticle = dbMob.ExamineArticle;
			MessageArticle = dbMob.MessageArticle;
			m_x = dbMob.X;
			m_y = dbMob.Y;
			m_z = dbMob.Z;
			_heading = (ushort) (dbMob.Heading & 0xFFF);
			MaxSpeedBase = (short) dbMob.Speed;
			CurrentRegionID = dbMob.Region;
			Realm = (eRealm)dbMob.Realm;
			Model = dbMob.Model;
			Size = dbMob.Size;
			Flags = (eFlags)dbMob.Flags;
			m_packageID = dbMob.PackageID;
			m_level = dbMob.Level;
			m_databaseLevel = dbMob.Level;
			AutoSetStats(dbMob);
			Level = dbMob.Level;
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

			if (dbMob.Brain != "")
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
					log.ErrorFormat("GameNPC error in LoadFromDatabase: can not instantiate brain of type {0} for npc {1}, name = {2}.", dbMob.Brain, dbMob.ClassType, dbMob.Name);
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

						if (Name != Name.ToLower())
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

					if (Name != Name.ToLower())
						aggroBrain.AggroLevel = 30;

					if (Realm != eRealm.None)
						aggroBrain.AggroLevel = 60;
				}
			}

			m_race = (short)dbMob.Race;
			m_bodyType = (ushort)dbMob.BodyType;
			m_houseNumber = (ushort)dbMob.HouseNumber;
			m_maxdistance = dbMob.MaxDistance;
			RoamingRange = dbMob.RoamingRange;
			m_isCloakHoodUp = dbMob.IsCloakHoodUp;
			m_visibleActiveWeaponSlots = dbMob.VisibleWeaponSlots;
			Gender = (eGender)dbMob.Gender;
			OwnerID = dbMob.OwnerID;

			LoadTemplate(NPCTemplate);
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
				Mob mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);
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

			Mob mob = null;

			if (InternalID != null)
				mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);

			if (mob == null)
			{
				if (LoadedFromScript == false)
					mob = new Mob();
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
				mob.FactionID = m_faction.ID;

			mob.MeleeDamageType = (int) MeleeDamageType;

			if (NPCTemplate != null)
				mob.NPCTemplateID = NPCTemplate.TemplateId;
			else
				mob.NPCTemplateID = -1;

			mob.Race = Race;
			mob.BodyType = BodyType;
			mob.PathID = PathID;
			mob.MaxDistance = m_maxdistance;
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

		/// <summary>
		/// Load a NPC template onto this NPC
		/// </summary>
		/// <param name="template"></param>
		public virtual void LoadTemplate(INpcTemplate template)
		{
			if (template == null)
				return;

			// Save the template for later
			NPCTemplate = template as NpcTemplate;

			// These stats aren't found in the mob table, so always get them from the template
			this.TetherRange = template.TetherRange;
			this.ParryChance = template.ParryChance;
			this.EvadeChance = template.EvadeChance;
			this.BlockChance = template.BlockChance;
			this.LeftHandSwingChance = template.LeftHandSwingChance;

			// We need level set before assigning spells to scale pet spells
			if (template.ReplaceMobValues)
			{
				byte choosenLevel = 1;
				if (!Util.IsEmpty(template.Level))
				{
					var split = Util.SplitCSV(template.Level, true);
					byte.TryParse(split[Util.Random(0, split.Count - 1)], out choosenLevel);
				}
				this.Level = choosenLevel; // Also calls AutosetStats()
			}

			if (template.Spells != null) this.Spells = template.Spells;
			if (template.Styles != null) this.Styles = template.Styles;
			if (template.Abilities != null)
			{
				lock (m_lockAbilities)
				{
					foreach (Ability ab in template.Abilities)
						m_abilities[ab.KeyName] = ab;
				}
			}

			// Everything below this point is already in the mob table
			if (!template.ReplaceMobValues && !LoadedFromScript)
				return;

			List<string> m_templatedInventory = new();
			TranslationId = template.TranslationId;
			Name = template.Name;
			Suffix = template.Suffix;
			GuildName = template.GuildName;
			ExamineArticle = template.ExamineArticle;
			MessageArticle = template.MessageArticle;
			Faction = FactionMgr.GetFactionByID(template.FactionId);

			#region Models, Sizes, Levels, Gender
			// Grav: this.Model/Size/Level accessors are triggering SendUpdate()
			// so i must use them, and not directly use private variables
			ushort choosenModel = 1;
			var splitModel = Util.SplitCSV(template.Model, true);
			ushort.TryParse(splitModel[Util.Random(0, splitModel.Count - 1)], out choosenModel);
			this.Model = choosenModel;

			// Graveen: template.Gender is 0,1 or 2 for respectively eGender.Neutral("it"), eGender.Male ("he"), 
			// eGender.Female ("she"). Any other value is randomly choosing a gender for current GameNPC
			int choosenGender = template.Gender > 2 ? Util.Random(0, 2) : template.Gender;

			switch (choosenGender)
			{
				default:
				case 0: this.Gender = eGender.Neutral; break;
				case 1: this.Gender = eGender.Male; break;
				case 2: this.Gender = eGender.Female; break;
			}

			byte choosenSize = 50;
			if (!Util.IsEmpty(template.Size))
			{
				var split = Util.SplitCSV(template.Size, true);
				byte.TryParse(split[Util.Random(0, split.Count - 1)], out choosenSize);
			}
			this.Size = choosenSize;
			#endregion

			#region Misc Stats
			MaxDistance = template.MaxDistance;
			Race = (short) template.Race;
			BodyType = template.BodyType;
			MaxSpeedBase = template.MaxSpeed;
			Flags = (eFlags)template.Flags;
			MeleeDamageType = template.MeleeDamageType;
			#endregion

			#region Inventory
			//Ok lets start loading the npc equipment - only if there is a value!
			if (!Util.IsEmpty(template.Inventory))
			{
				bool equipHasItems = false;
				GameNpcInventoryTemplate equip = new GameNpcInventoryTemplate();
				//First let's try to reach the npcequipment table and load that!
				//We use a ';' split to allow npctemplates to support more than one equipmentIDs
				var equipIDs = Util.SplitCSV(template.Inventory);
				if (!template.Inventory.Contains(":"))
				{

					foreach (string str in equipIDs)
					{
						m_templatedInventory.Add(str);
					}

					string equipid = "";

					if (m_templatedInventory.Count > 0)
					{
						if (m_templatedInventory.Count == 1)
							equipid = template.Inventory;
						else
							equipid = m_templatedInventory[Util.Random(m_templatedInventory.Count - 1)];
					}
					if (equip.LoadFromDatabase(equipid))
						equipHasItems = true;
				}

				#region Legacy Equipment Code
				//Nope, nothing in the npcequipment table, lets do the crappy parsing
				//This is legacy code
				if (!equipHasItems && template.Inventory.Contains(":"))
				{
					//Temp list to store our models
					List<int> tempModels = new List<int>();

					//Let's go through all of our ';' seperated slots
					foreach (string str in equipIDs)
					{
						tempModels.Clear();
						//Split the equipment into slot and model(s)
						string[] slotXModels = str.Split(':');
						//It should only be two in length SLOT : MODELS
						if (slotXModels.Length == 2)
						{
							int slot;
							//Let's try to get our slot
							if (Int32.TryParse(slotXModels[0], out slot))
							{
								//Now lets go through and add all the models to the list
								string[] models = slotXModels[1].Split('|');
								foreach (string strModel in models)
								{
									//We'll add it to the list if we successfully parse it!
									int model;
									if (Int32.TryParse(strModel, out model))
										tempModels.Add(model);
								}

								//If we found some models let's randomly pick one and add it the equipment
								if (tempModels.Count > 0)
									equipHasItems |= equip.AddNPCEquipment((eInventorySlot)slot, tempModels[Util.Random(tempModels.Count - 1)]);
							}
						}
					}
				}
				#endregion

				//We added some items - let's make it the new inventory
				if (equipHasItems)
				{
					this.Inventory = new GameNPCInventory(equip);
					if (this.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
						this.SwitchWeapon(eActiveWeaponSlot.Distance);
					else
					{
						InventoryItem twohand = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
						InventoryItem onehand = Inventory.GetItem(eInventorySlot.RightHandWeapon);

						if (twohand != null && onehand != null)
							//Let's add some random chance
							SwitchWeapon(Util.Chance(50) ? eActiveWeaponSlot.TwoHanded : eActiveWeaponSlot.Standard);
						else if (twohand != null)
							//Hmm our right hand weapon may have been null
							SwitchWeapon(eActiveWeaponSlot.TwoHanded);
						else if (onehand != null)
							//Hmm twohand was null lets default down here
							SwitchWeapon(eActiveWeaponSlot.Standard);

					}
				}

				if (template.VisibleActiveWeaponSlot > 0)
					this.VisibleActiveWeaponSlots = template.VisibleActiveWeaponSlot;
			}
			#endregion

			BuffBonusCategory4[(int)eStat.STR] += template.Strength;
			BuffBonusCategory4[(int)eStat.DEX] += template.Dexterity;
			BuffBonusCategory4[(int)eStat.CON] += template.Constitution;
			BuffBonusCategory4[(int)eStat.QUI] += template.Quickness;
			BuffBonusCategory4[(int)eStat.INT] += template.Intelligence;
			BuffBonusCategory4[(int)eStat.PIE] += template.Piety;
			BuffBonusCategory4[(int)eStat.EMP] += template.Empathy;
			BuffBonusCategory4[(int)eStat.CHR] += template.Charisma;

			m_ownBrain = new StandardMobBrain
			{
				Body = this,
				AggroLevel = template.AggroLevel,
				AggroRange = template.AggroRange
			};
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
			base.SwitchWeapon(slot);
			if (ObjectState == eObjectState.Active)
			{
				// Update active weapon appearence
				BroadcastLivingEquipmentUpdate();
			}
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
			lock (m_questListToGive.SyncRoot)
			{
				if (HasQuest(questType) == null)
				{
					AbstractQuest newQuest = (AbstractQuest)Activator.CreateInstance(questType);
					if (newQuest != null) m_questListToGive.Add(newQuest);
				}
			}
		}

		/// <summary>
		/// removes a scripted quest from this npc
		/// </summary>
		/// <param name="questType">The questType to remove</param>
		/// <returns>true if added, false if the npc has already the quest!</returns>
		public bool RemoveQuestToGive(Type questType)
		{
			lock (m_questListToGive.SyncRoot)
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
			lock (m_questListToGive.SyncRoot)
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
			lock (m_questListToGive.SyncRoot)
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
			lock (m_dataQuests)
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
			foreach (AbstractQuest quest in player.QuestList.Keys)
			{
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
			lock (m_questListToGive.SyncRoot)
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

		public override void OnUpdateByPlayerService()
		{
			m_lastVisibleToPlayerTick = GameLoop.GameLoopTime;

			if (Brain != null && !Brain.EntityManagerId.IsSet)
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

			bool anyPlayer = false;

			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					continue;

				player.Out.SendNPCCreate(this);

				if (m_inventory != null)
					player.Out.SendLivingEquipmentUpdate(this);

				anyPlayer = true;
			}

			if (anyPlayer)
				m_lastVisibleToPlayerTick = GameLoop.GameLoopTime;

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
				log.Info("NPC '" + Name + "' added to house " + m_houseNumber);
				CurrentHouse = HouseMgr.GetHouse(m_houseNumber);

				if (CurrentHouse == null)
					log.Warn("House " + CurrentHouse + " for NPC " + Name + " doesn't exist");
				else
					log.Info("Confirmed number: " + CurrentHouse.HouseNumber.ToString());
			}

			if (!InCombat && IsAlive && base.Health < MaxHealth)
				base.Health = MaxHealth;

			BuildAmbientTexts();

			if (GameServer.Instance.ServerStatus == eGameServerStatus.GSS_Open)
				FireAmbientSentence(eAmbientTrigger.spawning, this);

			if (ShowTeleporterIndicator)
			{
				if (m_teleporterIndicator == null)
				{
					m_teleporterIndicator = new GameNPC
					{
						Name = "",
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

			if (Flags.HasFlag(eFlags.STEALTH))
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
				if (Brain is ControlledNpcBrain controlledBrain && controlledBrain.WalkState != eWalkState.Follow)
					return false;
			}

			Region rgn = WorldMgr.GetRegion(regionID);

			if (rgn == null || rgn.GetZone(x, y) == null)
				return false;

			Notify(GameObjectEvent.MoveTo, this, new MoveToEventArgs(regionID, x, y, z, heading));

			HashSet<GamePlayer> playersInRadius = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);

			m_x = x;
			m_y = y;
			m_z = z;
			_heading = heading;

			// Previous position.
			foreach (GamePlayer player in playersInRadius)
				player.Out.SendObjectRemove(this);

			// New position.
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					continue;

				player.Out.SendNPCCreate(this);

				if (m_inventory != null)
					player.Out.SendLivingEquipmentUpdate(this);
			}

			return true;
		}

		/// <summary>
		/// Gets or Sets the current Region of the Object
		/// </summary>
		public override Region CurrentRegion
		{
			get => base.CurrentRegion;
			set
			{
				Region oldRegion = CurrentRegion;
				base.CurrentRegion = value;
				Region newRegion = CurrentRegion;
			}
		}

		/// <summary>
		/// Marks this object as deleted!
		/// </summary>
		public override void Delete()
		{
			lock (m_respawnTimerLock)
			{
				if (m_respawnTimer != null)
				{
					m_respawnTimer.Stop();
					m_respawnTimer = null;
				}
			}

			Brain.Stop();
			StopFollowing();
			TempProperties.removeProperty(CHARMED_TICK_PROP);
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
				throw new ArgumentException("The new brain is already active.", "brain");

			ABrain oldBrain = m_ownBrain;
			bool activate = oldBrain.IsActive;
			if (activate)
				oldBrain.Stop();
			m_ownBrain = brain;
			m_ownBrain.Body = this;
			if (activate)
				m_ownBrain.Start();

			return oldBrain;
		}

		/// <summary>
		/// Adds a temporary brain to Npc, last added brain is active
		/// </summary>
		/// <param name="newBrain"></param>
		public virtual void AddBrain(ABrain newBrain)
		{
			if (newBrain == null)
				throw new ArgumentNullException("newBrain");
			if (newBrain.IsActive)
				throw new ArgumentException("The new brain is already active.", "newBrain");

			Brain.Stop();
			ArrayList brains = new ArrayList(m_brains);
			brains.Add(newBrain);
			m_brains = brains; // make new array list to avoid locks in the Brain property
			newBrain.Body = this;
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
			{
				//Console.WriteLine("removeBrain is null!");
				return false;
			}

			ArrayList brains = new ArrayList(m_brains);
			int index = brains.IndexOf(removeBrain);
			if (index < 0)
			{
				//Console.WriteLine("Brain index < 0");
				return false;
			}
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
			// "aggressive", "hostile", "neutral", "friendly"
			// TODO: correct aggro strings
			// TODO: some merchants can be aggressive to players even in same realm
			// TODO: findout if trainers can be aggro at all

			//int aggro = CalculateAggroLevelToTarget(player);

			// "aggressive towards you!", "hostile towards you.", "neutral towards you.", "friendly."
			// TODO: correct aggro strings
			string aggroLevelString = "";
			int aggroLevel;
			IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;
			//Calculate Faction aggro - base AggroLevel needs to be greater tha 0 for Faction aggro calc to work.
			if (Faction != null && aggroBrain != null && aggroBrain.AggroLevel > 0 && aggroBrain.AggroRange > 0)
			{
				aggroLevel = Faction.GetAggroToFaction(player);
				
				if (GameServer.ServerRules.IsSameRealm(this, player, true))
				{
					if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly2");
					else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
				}
				else if (aggroLevel > 75)
					aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive1");
				else if (aggroLevel > 50)
					aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Hostile1");
				else if (aggroLevel > 25)
					aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral1");
				else
					aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
			}
			else
			{
				if (GameServer.ServerRules.IsSameRealm(this, player, true))
				{
					if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly2");
					else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
				}
				else if (aggroBrain != null && aggroBrain.AggroLevel > 0)
				{
					if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive2");
					else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive1");
				}
				else
				{
					if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral2");
					else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral1");
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
			switch (player.Client.Account.Language)
			{
				case "EN":
				{
					IList list = base.GetExamineMessages(player);
					// Message: You examine {0}. {1} is {2}.
					list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetExamineMessages.YouExamine", GetName(0, false), GetPronoun(0, true), GetAggroLevelString(player, false)));
					return list;
				}
				default:
					{
						IList list = new ArrayList(4);
						// Message: You examine {0}. {1} is {2}.
						list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetExamineMessages.YouExamine",
															GetName(0, false, player.Client.Account.Language, this),
															GetPronoun(0, true, player.Client.Account.Language), GetAggroLevelString(player, false)));
						return list;
					}
			}
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
		public IList<MobXAmbientBehaviour> ambientTexts;

		/// <summary>
		/// This function is called from the ObjectInteractRequestHandler
		/// </summary>
		/// <param name="player">GamePlayer that interacts with this object</param>
		/// <returns>false if interaction is prevented</returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			//if (!GameServer.ServerRules.IsSameRealm(this, player, true) && Faction.GetAggroToFaction(player) > 25)
			if (!GameServer.ServerRules.IsSameRealm(this, player, true) && Faction != null && Faction.GetAggroToFaction(player) > 50)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.DirtyLook",
					GetName(0, true, player.Client.Account.Language, this)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

				Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
				return false;
			}
			if (MAX_PASSENGERS > 1)
			{
				string name = "";
				if (this is GameTaxiBoat)
					name = "boat";
				if (this is GameSiegeRam)
					name = "ram";

				if (this is GameSiegeRam && player.Realm != this.Realm)
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
				{
					player.DismountSteed(true);
				}

				if (player.IsOnHorse)
				{
					player.IsOnHorse = false;
				}

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
			if (source is GamePlayer == false)
				return true;

			// GamePlayer player = (GamePlayer)source;
			//
			// //TODO: Guards in rvr areas doesn't need check
			// if (text == "task")
			// {
			// 	if (source.TargetObject == null)
			// 		return false;
			// 	if (KillTask.CheckAvailability(player, (GameLiving)source.TargetObject))
			// 	{
			// 		KillTask.BuildTask(player, (GameLiving)source.TargetObject);
			// 		return true;
			// 	}
			// 	else if (MoneyTask.CheckAvailability(player, (GameLiving)source.TargetObject))
			// 	{
			// 		MoneyTask.BuildTask(player, (GameLiving)source.TargetObject);
			// 		return true;
			// 	}
			// 	else if (CraftTask.CheckAvailability(player, (GameLiving)source.TargetObject))
			// 	{
			// 		CraftTask.BuildTask(player, (GameLiving)source.TargetObject);
			// 		return true;
			// 	}
			// }
			return true;
		}

		public override bool ReceiveItem(GameLiving source, InventoryItem item)
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

			if (FollowTarget != target)
			{
				StopFollowing();
				Follow(target, movementComponent.FollowMinDistance, movementComponent.FollowMaxDistance);
			}

			FireAmbientSentence(eAmbientTrigger.fighting, target);
		}

		private int scalingFactor = Properties.GAMENPC_SCALING;
		private int orbsReward = 0;
		
		public override double GetWeaponSkill(InventoryItem weapon)
		{
			// https://camelotherald.fandom.com/wiki/Weapon_Skill
			// [[[[LEVEL *DAMAGE_TABLE * (200 + BONUS * ITEM_BONUS) / 500]
			// (100 + STAT) / 100]
			// (100 + SPEC) / 100]
			// (100 + WEAPONSKILL_BONUS) / 100]
			
			int weaponskill = (Level + 1) 
				* (ScalingFactor / 4) // Mob damage table calc, basically.
				* (200 + GetModified(eProperty.MeleeDamage)) / 500 // Melee damage buffs.
				* ((100 + Strength) / 100) // NPCs only use STR to calculate, can skip str or str/dex check.
				* ((100 + GetModified(eProperty.WeaponSkill)) / 100); // WeaponSkill buffs.
  
			return weaponskill;
		}

		public void SetLastMeleeAttackTick()
		{
			if (TargetObject?.Realm == 0 || Realm == 0)
				m_lastAttackTickPvE = GameLoop.GameLoopTime;
			else
				m_lastAttackTickPvP = GameLoop.GameLoopTime;
		}

		/// <summary>
		/// Returns the Damage this NPC does on an attack, adding 2H damage bonus if appropriate
		/// </summary>
		/// <param name="weapon">the weapon used for attack</param>
		/// <returns></returns>
		public virtual double AttackDamage(InventoryItem weapon)
		{
			return attackComponent.AttackDamage(weapon);
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
					CurrentSpeed = MaxSpeed;
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
		public virtual bool IsWorthReward
		{
			get
			{
				if (CurrentRegion == null || CurrentRegion.Time - CHARMED_NOEXP_TIMEOUT < TempProperties.getProperty<long>(CHARMED_TICK_PROP))
					return false;
				if (this.Brain is IControlledBrain)
					return false;
				
				HybridDictionary XPGainerList = new HybridDictionary();
				lock (m_xpGainers.SyncRoot)
				{
					foreach (DictionaryEntry gainer in m_xpGainers)
					{
						XPGainerList.Add(gainer.Key, gainer.Value);
					}
				}
				if (XPGainerList.Keys.Count == 0) return false;
				foreach (DictionaryEntry de in XPGainerList)
				{
					GameObject obj = (GameObject)de.Key;
					if (obj is GamePlayer)
					{
						//If a gameplayer with privlevel > 1 attacked the
						//mob, then the players won't gain xp ...
						if (((GamePlayer)obj).Client.Account.PrivLevel > 1)
							return false;
						//If a player to which we are gray killed up we
						//aren't worth anything either
						if (((GamePlayer)obj).IsObjectGreyCon(this))
							return false;
					}
					else
					{
						//If object is no gameplayer and realm is != none
						//then it means that a npc has hit this living and
						//it is not worth any xp ...
						//if(obj.Realm != (byte)eRealm.None)
						//If grey to at least one living then no exp
						if (obj is GameLiving && ((GameLiving)obj).IsObjectGreyCon(this))
							return false;
					}
				}
				return true;
				
			}
			set
			{
			}
		}

		protected void ControlledNPC_Release()
		{
			if (this.ControlledBrain != null)
			{
				//log.Info("On tue le pet !");
				this.Notify(GameLivingEvent.PetReleased, ControlledBrain.Body);
			}
		}

		/// <summary>
		/// Called when this living dies
		/// </summary>
		public override void ProcessDeath(GameObject killer)
		{
			int hashCode = GetHashCode();

			try
			{
				Brain?.KillFSM();

				FireAmbientSentence(eAmbientTrigger.dying, killer);

				if (ControlledBrain != null)
					ControlledNPC_Release();

				if (killer != null)
				{
					if (killer is GameNPC pet && pet.Brain is IControlledBrain petBrain)
						killer = petBrain.GetPlayerOwner();

					Diagnostics.StartPerfCounter($"ReaperService-NPC-ProcessDeath-DropLoot-NPC({hashCode})");

					if (IsWorthReward)
						DropLoot(killer);

					Diagnostics.StopPerfCounter($"ReaperService-NPC-ProcessDeath-DropLoot-NPC({hashCode})");
					Diagnostics.StartPerfCounter($"ReaperService-NPC-ProcessDeath-AreaMessages-NPC({hashCode})");

					Message.SystemToArea(this, GetName(0, true) + " dies!", eChatType.CT_PlayerDied, killer);

					if (killer is GamePlayer player)
						player.Out.SendMessage(GetName(0, true) + " dies!", eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow);

					Diagnostics.StopPerfCounter($"ReaperService-NPC-ProcessDeath-AreaMessages-NPC({hashCode})");
				}

				StopMoving();

				if (Group != null)
					Group.RemoveMember(this);

				if (killer != null)
				{
					// Handle faction alignement changes.
					if (Faction != null && killer is GamePlayer)
					{
						lock (m_xpGainers.SyncRoot)
						{
							foreach (GameLiving xpGainer in m_xpGainers.Keys)
							{
								GamePlayer playerXpGainer = xpGainer as GamePlayer;

								if (playerXpGainer != null && playerXpGainer.IsObjectGreyCon(this))
									continue;

								if (playerXpGainer != null &&
									playerXpGainer.ObjectState == eObjectState.Active &&
									playerXpGainer.IsAlive &&
									playerXpGainer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
									Faction.KillMember(playerXpGainer);
							}
						}
					}

					// Deal out exp and realm points based on server rules.
					Diagnostics.StartPerfCounter($"ReaperService-NPC-ProcessDeath-OnNPCKIlled-NPC({hashCode})");
					GameServer.ServerRules.OnNPCKilled(this, killer);
					Diagnostics.StopPerfCounter($"ReaperService-NPC-ProcessDeath-OnNPCKIlled-NPC({hashCode})");
				}

				base.ProcessDeath(killer);

				lock (XPGainers.SyncRoot)
				{
					XPGainers.Clear();
				}

				Delete();
				TempProperties.removeAllProperties();
				StartRespawn();
			}
			finally
			{
				if (isDeadOrDying == true)
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

		/// <summary>
		/// Method to switch the npc to Melee attacks
		/// </summary>
		/// <param name="target"></param>
		public void SwitchToMelee(GameObject target)
		{
			// Tolakram: Order is important here.  First StopAttack, then switch weapon
			StopFollowing();
			attackComponent.StopAttack();

			InventoryItem twohand = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
			InventoryItem righthand = Inventory.GetItem(eInventorySlot.RightHandWeapon);

			if (twohand != null && righthand == null)
				SwitchWeapon(eActiveWeaponSlot.TwoHanded);
			else if (twohand != null && righthand != null)
			{
				if (Util.Chance(50))
					SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				else SwitchWeapon(eActiveWeaponSlot.Standard);
			}
			else
				SwitchWeapon(eActiveWeaponSlot.Standard);

			attackComponent.RequestStartAttack(target);
		}

		/// <summary>
		/// Method to switch the guard to Ranged attacks
		/// </summary>
		/// <param name="target"></param>
		public void SwitchToRanged(GameObject target)
		{
			StopFollowing();
			attackComponent.StopAttack();
			SwitchWeapon(eActiveWeaponSlot.Distance);
			attackComponent.RequestStartAttack(target);
		}

		public override void StartInterruptTimer(int duration, AttackData.eAttackType attackType, GameLiving attacker)
		{
			// Increase substantially the base interrupt timer duration for non player controlled NPCs
			// so that they don't start attacking immediately after the attacker's melee swing interval.
			// It makes repositioning them easier without having to constantly attack them.
			if (Brain is not IControlledBrain controlledBrain || controlledBrain.GetPlayerOwner() == null)
				duration += 2500;

			base.StartInterruptTimer(duration, attackType, attacker);
		}

		protected override bool CheckRangedAttackInterrupt(GameLiving attacker, AttackData.eAttackType attackType)
		{
			// Immobile NPCs can only be interrupted from close range attacks.
			if (MaxSpeedBase == 0 && attackType is AttackData.eAttackType.Ranged or AttackData.eAttackType.Spell && !IsWithinRadius(attacker, 150))
				return false;

			bool interrupted = base.CheckRangedAttackInterrupt(attacker, attackType);

			if (interrupted)
				attackComponent.attackAction?.OnAimInterrupt(attacker);

			return interrupted;
		}

		public override bool StartInterruptTimerOnItselfOnMeleeAttack()
		{
			return false;
		}

		/// <summary>
		/// The time to wait before each mob respawn
		/// </summary>
		protected int m_respawnInterval = -1;
		/// <summary>
		/// A timer that will respawn this mob
		/// </summary>
		protected AuxECSGameTimer m_respawnTimer;
		/// <summary>
		/// The sync object for respawn timer modifications
		/// </summary>
		protected readonly object m_respawnTimerLock = new object();

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

			int respawnInt = RespawnInterval;
			int minBound = (int) Math.Floor(respawnInt * .95);
			int maxBound = (int) Math.Floor(respawnInt * 1.05);
			respawnInt = Util.Random(minBound, maxBound);
			if (respawnInt > 0)
			{
				lock (m_respawnTimerLock)
				{
					if (m_respawnTimer == null)
					{
						m_respawnTimer = new AuxECSGameTimer(this);
						m_respawnTimer.Callback = new AuxECSGameTimer.AuxECSTimerCallback(RespawnTimerCallback);
					}
					else if (m_respawnTimer.IsAlive)
					{
						m_respawnTimer.Stop();
					}
					// register Mob as "respawning"
					CurrentRegion.MobsRespawning.TryAdd(this, respawnInt);

					m_respawnTimer.Start(respawnInt);
				}
			}
		}

		/// <summary>
		/// The callback that will respawn this mob
		/// </summary>
		/// <param name="respawnTimer">the timer calling this callback</param>
		/// <returns>the new interval</returns>
		protected virtual int RespawnTimerCallback(AuxECSGameTimer respawnTimer)
		{
			CurrentRegion.MobsRespawning.TryRemove(this, out _);

			lock (m_respawnTimerLock)
			{
				if (m_respawnTimer != null)
				{
					m_respawnTimer.Stop();
					m_respawnTimer = null;
				}
			}

			if (IsAlive || ObjectState == eObjectState.Active)
				return 0;

			/*
			if (m_level >= 5 && m_databaseLevel < 60)
			{
				int minBound = (int) Math.Round(m_databaseLevel * .9);
				int maxBound = (int) Math.Round(m_databaseLevel * 1.1);
				this.Level = (byte)  Util.Random(minBound, maxBound);
			}*/

			SpawnTick = GameLoop.GameLoopTime;

			// Heal this NPC and move it to the spawn location.
			Health = MaxHealth;
			Mana = MaxMana;
			Endurance = MaxEndurance;

			int origSpawnX = m_spawnPoint.X;
			int origSpawnY = m_spawnPoint.Y;
			X = m_spawnPoint.X;
			Y = m_spawnPoint.Y;
			Z = m_spawnPoint.Z;
			Heading = m_spawnHeading;
			AddToWorld();
			m_spawnPoint.X = origSpawnX;
			m_spawnPoint.Y = origSpawnY;

			// Delay the first think tick a bit to prevent clients from sending positive LoS check
			// when they shouldn't, which can happen right after 'SendNPCCreate' and makes mobs aggro through walls.
			Brain.LastThinkTick = GameLoop.GameLoopTime + 1250;
			return 0;
		}

		/// <summary>
		/// Callback timer for health regeneration
		/// </summary>
		/// <param name="selfRegenerationTimer">the regeneration timer</param>
		/// <returns>the new interval</returns>
		protected override int HealthRegenerationTimerCallback(ECSGameTimer selfRegenerationTimer)
		{
			int period = base.HealthRegenerationTimerCallback(selfRegenerationTimer);

			if (!InCombat)
			{
				int oldPercent = HealthPercent;

				if (oldPercent != HealthPercent)
					NeedsBroadcastUpdate = true;
			}

			return period;
		}

		/// <summary>
		/// The chance for a critical hit
		/// </summary>
		public int AttackCriticalChance(InventoryItem weapon)
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
			if (Brain is StandardMobBrain standardMobBrain)
				standardMobBrain.OnAttackedByEnemy(ad);

			if ((Flags & eFlags.STEALTH) != 0)
				Flags ^= eFlags.STEALTH;

			base.OnAttackedByEnemy(ad);
		}

		/// <summary>
		/// This method is called to drop loot after this mob dies
		/// </summary>
		/// <param name="killer">The killer</param>
		public virtual void DropLoot(GameObject killer)
		{
			// TODO: mobs drop "a small chest" sometimes
			ArrayList droplist = new ArrayList();
			ArrayList autolootlist = new ArrayList();
			ArrayList aplayer = new ArrayList();
			
			HybridDictionary XPGainerList = new HybridDictionary();
			lock (m_xpGainers.SyncRoot)
			{
				foreach (DictionaryEntry gainer in m_xpGainers)
				{
					XPGainerList.Add(gainer.Key, gainer.Value);
				}
			}
			
			if (XPGainerList.Keys.Count == 0) return;

			ItemTemplate[] lootTemplates = LootMgr.GetLoot(this, killer);

			foreach (ItemTemplate lootTemplate in lootTemplates)
			{
				if (lootTemplate == null) continue;
				GameStaticItem loot = null;
				if (GameMoney.IsItemMoney(lootTemplate.Name))
				{
					long value = lootTemplate.Price;
					//GamePlayer killerPlayer = killer as GamePlayer;

					//[StephenxPimentel] - Zone Bonus XP Support
					if (ServerProperties.Properties.ENABLE_ZONE_BONUSES)
					{
						GamePlayer killerPlayer = killer as GamePlayer;
						if (killer is GameNPC)
						{
							if (killer is GameNPC && ((killer as GameNPC).Brain is IControlledBrain))
								killerPlayer = ((killer as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
							else return;
						}

						int zoneBonus = (((int)value * ZoneBonus.GetCoinBonus(killerPlayer) / 100));
						if (zoneBonus > 0)
						{
							long amount = (long)(zoneBonus * ServerProperties.Properties.MONEY_DROP);
							killerPlayer.AddMoney(amount,
												  ZoneBonus.GetBonusMessage(killerPlayer, (int)(zoneBonus * ServerProperties.Properties.MONEY_DROP), ZoneBonus.eZoneBonusType.COIN),
												  eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							InventoryLogging.LogInventoryAction(this, killerPlayer, eInventoryActionType.Loot, amount);
						}
					}

					if (Keeps.KeepBonusMgr.RealmHasBonus(DOL.GS.Keeps.eKeepBonusType.Coin_Drop_5, (eRealm)killer.Realm))
						value += (value / 100) * 5;
					else if (Keeps.KeepBonusMgr.RealmHasBonus(DOL.GS.Keeps.eKeepBonusType.Coin_Drop_3, (eRealm)killer.Realm))
						value += (value / 100) * 3;

					//this will need to be changed when the ML for increasing money is added
					if (value != lootTemplate.Price)
					{
						GamePlayer killerPlayer = killer as GamePlayer;
						if (killerPlayer != null)
							killerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(killerPlayer.Client, "GameNPC.DropLoot.AdditionalMoney", Money.GetString(value - lootTemplate.Price)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
					}

					//Mythical Coin bonus property (Can be used for any equipped item, bonus 235)
					if (killer is GamePlayer)
					{
						GamePlayer killerPlayer = killer as GamePlayer;
						if (killerPlayer.GetModified(eProperty.MythicalCoin) > 0)
						{
							value += (value * killerPlayer.GetModified(eProperty.MythicalCoin)) / 100;
							killerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(killerPlayer.Client,
																					"GameNPC.DropLoot.ItemAdditionalMoney", Money.GetString(value - lootTemplate.Price)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
						}
					}

					loot = new GameMoney(value, this);
					loot.Name = lootTemplate.Name;
					loot.Model = (ushort)lootTemplate.Model;
				}
				else
				{
					InventoryItem invitem;

					if (lootTemplate is ItemUnique)
					{
						GameServer.Database.AddObject(lootTemplate);
						invitem = GameInventoryItem.Create(lootTemplate as ItemUnique);
					}
					else
						invitem = GameInventoryItem.Create(lootTemplate);

					if (lootTemplate is GeneratedUniqueItem)
					{
						invitem.IsROG = true;
					}

					loot = new WorldInventoryItem(invitem);
					loot.X = X;
					loot.Y = Y;
					loot.Z = Z;
					loot.Heading = Heading;
					loot.CurrentRegion = CurrentRegion;
					(loot as WorldInventoryItem).Item.IsCrafted = false;
					(loot as WorldInventoryItem).Item.Creator = Name;

					// This may seem like an odd place for this code, but loot-generating code further up the line
					// is dealing strictly with ItemTemplate objects, while you need the InventoryItem in order
					// to be able to set the Count property.
					// Converts single drops of loot with PackSize > 1 (and MaxCount >= PackSize) to stacks of Count = PackSize
					if (((WorldInventoryItem)loot).Item.PackSize > 1 && ((WorldInventoryItem)loot).Item.MaxCount >= ((WorldInventoryItem)loot).Item.PackSize)
					{
						((WorldInventoryItem)loot).Item.Count = ((WorldInventoryItem)loot).Item.PackSize;
					}
				}

				GamePlayer playerAttacker = null;
				BattleGroup activeBG = null;
				if (killer is GamePlayer playerKiller &&
					playerKiller.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null) != null)
					activeBG = playerKiller.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
				
				foreach (GameObject gainer in XPGainerList.Keys)
				{
					//if a battlegroup killed the mob, filter out any non BG players
					if (activeBG != null && gainer is GamePlayer p &&
						p.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null) != activeBG)
						continue;
					
					if (gainer is GamePlayer)
					{
						playerAttacker = gainer as GamePlayer;
						if (loot.Realm == 0)
							loot.Realm = ((GamePlayer)gainer).Realm;
					}
					loot.AddOwner(gainer);
					if (gainer is GameNPC)
					{
						IControlledBrain brain = ((GameNPC)gainer).Brain as IControlledBrain;
						if (brain != null)
						{
							playerAttacker = brain.GetPlayerOwner();
							loot.AddOwner(brain.GetPlayerOwner());
						}
					}
				}
				if (playerAttacker == null) return; // no loot if mob kills another mob


				droplist.Add(loot.GetName(1, false));
				Diagnostics.StartPerfCounter("ReaperService-NPC-DropLoot-AddToWorld-loot("+loot.GetHashCode()+")");
				loot.AddToWorld();
				Diagnostics.StopPerfCounter("ReaperService-NPC-DropLoot-AddToWorld-loot("+loot.GetHashCode()+")");

				foreach (GameObject gainer in XPGainerList.Keys)
				{
					if (gainer is GamePlayer)
					{
						GamePlayer player = gainer as GamePlayer;
						if (player.Autoloot && loot.IsWithinRadius(player, 2400)) // should be large enough for most casters to autoloot
						{
							if (player.Group == null || (player.Group != null && player == player.Group.Leader))
								aplayer.Add(player);
							autolootlist.Add(loot);
						}
					}
				}
			}

			Diagnostics.StartPerfCounter("ReaperService-NPC-DropLoot-BroadcastLoot-npc("+this.GetHashCode()+")");
			BroadcastLoot(droplist);
			Diagnostics.StopPerfCounter("ReaperService-NPC-DropLoot-BroadcastLoot-npc("+this.GetHashCode()+")");

			Diagnostics.StartPerfCounter("ReaperService-NPC-DropLoot-PickupLoot-npc("+this.GetHashCode()+")");
			if (autolootlist.Count > 0)
			{
				foreach (GameObject obj in autolootlist)
				{
					foreach (GamePlayer player in aplayer)
					{
						player.PickupObject(obj, true);
						break;
					}
				}
			}
			Diagnostics.StopPerfCounter("ReaperService-NPC-DropLoot-PickupLoot-npc("+this.GetHashCode()+")");
		}

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

			var attackerLiving = healSource as GameLiving;
			if (attackerLiving == null)
				return;

			Group attackerGroup = attackerLiving.Group;
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
				this.AddXPGainer(healSource, (float)healAmount);
			}

			if (Brain is StandardMobBrain mobBrain)
			{
				// first check to see if the healer is in our aggrolist so we don't go attacking anyone who heals
				if (mobBrain.AggroTable.ContainsKey(healSource as GameLiving))
				{
					if (healSource is GamePlayer || (healSource is GameNPC && (((GameNPC)healSource).Flags & eFlags.PEACE) == 0))
					{
						mobBrain.AddToAggroList((GameLiving)healSource, healAmount);
					}
				}
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

		private List<Spell> m_spells = new(0);
		private ConcurrentDictionary<GameObject, (Spell, SpellLine, long)> m_castSpellLosChecks = new();
		private bool m_spellCastedFromLosCheck;

		/// <summary>
		/// property of spell array of NPC
		/// </summary>
		public virtual IList Spells
		{
			get { return m_spells; }
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
					m_spells = value.Cast<Spell>().ToList();
					//if(!SortedSpells)
						SortSpells();
				}
			}
		}

		/// <summary>
		/// Harmful spell list and accessor
		/// </summary>
		public List<Spell> HarmfulSpells { get; set; } = null;

		/// <summary>
		/// Whether or not the NPC can cast harmful spells with a cast time.
		/// </summary>
		public bool CanCastHarmfulSpells
		{
			get { return (HarmfulSpells != null && HarmfulSpells.Count > 0); }
		}

		/// <summary>
		/// Instant harmful spell list and accessor
		/// </summary>
		public List<Spell> InstantHarmfulSpells { get; set; } = null;

		/// <summary>
		/// Whether or not the NPC can cast harmful instant spells.
		/// </summary>
		public bool CanCastInstantHarmfulSpells
		{
			get { return (InstantHarmfulSpells != null && InstantHarmfulSpells.Count > 0); }
		}

		/// <summary>
		/// Healing spell list and accessor
		/// </summary>
		public List<Spell> HealSpells { get; set; } = null;

		/// <summary>
		/// Whether or not the NPC can cast heal spells with a cast time.
		/// </summary>
		public bool CanCastHealSpells
		{
			get { return (HealSpells != null && HealSpells.Count > 0); }
		}

		/// <summary>
		/// Instant healing spell list and accessor
		/// </summary>
		public List<Spell> InstantHealSpells { get; set; } = null;

		/// <summary>
		/// Whether or not the NPC can cast instant healing spells.
		/// </summary>
		public bool CanCastInstantHealSpells
		{
			get { return (InstantHealSpells != null && InstantHealSpells.Count > 0); }
		}

		/// <summary>
		/// Miscellaneous spell list and accessor
		/// </summary>
		public List<Spell> MiscSpells { get; set; } = null;

		/// <summary>
		/// Whether or not the NPC can cast miscellaneous spells with a cast time.
		/// </summary>
		public bool CanCastMiscSpells
		{
			get { return (MiscSpells != null && MiscSpells.Count > 0); }
		}

		/// <summary>
		/// Instant miscellaneous spell list and accessor
		/// </summary>
		public List<Spell> InstantMiscSpells { get; set; } = null;

		/// <summary>
		/// Whether or not the NPC can cast miscellaneous instant spells.
		/// </summary>
		public bool CanCastInstantMiscSpells
		{
			get { return (InstantMiscSpells != null && InstantMiscSpells.Count > 0); }
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
			} // foreach

			//SortedSpells = true;
		}

		/// <summary>
		/// Cast a spell, with optional LOS check
		/// </summary>
		public virtual bool CastSpell(Spell spell, SpellLine line, bool checkLos)
		{
			bool casted;

			if (IsIncapacitated)
				return false;

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
		/// Cast a spell with LOS check to a player
		/// </summary>
		/// <returns>Whether the spellcast started successfully</returns>
		public override bool CastSpell(Spell spell, SpellLine line)
		{
			// Good opportunity to clean up our 'm_spellTargetLosChecks'.
			// Entries older than 3 seconds are removed, so that another check can be performed in case the previous one never was.
			for (int i = m_castSpellLosChecks.Count - 1; i >= 0; i--)
			{
				var element = m_castSpellLosChecks.ElementAt(i);

				if (GameLoop.GameLoopTime - element.Value.Item3 >= 3000)
					m_castSpellLosChecks.TryRemove(element.Key, out _);
			}

			if (IsIncapacitated)
				return false;

			Spell spellToCast = null;

			if (line.KeyName == GlobalSpellsLines.Mob_Spells)
			{
				// NPC spells will get the level equal to their caster
				spellToCast = (Spell) spell.Clone();
				spellToCast.Level = Level;
			}
			else
				spellToCast = spell;

			if (TargetObject == this || TargetObject == null)
				return base.CastSpell(spellToCast, line);

			if (spellToCast.Range > 0 && !IsWithinRadius(TargetObject, spellToCast.Range))
				return false;

			GamePlayer LosChecker = TargetObject as GamePlayer;

			if (LosChecker == null && Brain is IControlledBrain brain)
				LosChecker = brain.GetPlayerOwner();

			if (LosChecker == null)
			{
				foreach (GamePlayer playerInRange in GetPlayersInRadius(350))
				{
					if (playerInRange != null)
					{
						LosChecker = playerInRange;
						break;
					}
				}
			}

			if (LosChecker == null)
				return base.CastSpell(spellToCast, line);

			bool spellCastedFromLosCheck = m_spellCastedFromLosCheck;

			if (spellCastedFromLosCheck)
				m_spellCastedFromLosCheck = false;

			if (m_castSpellLosChecks.TryAdd(TargetObject, new(spellToCast, line, GameLoop.GameLoopTime)))
				LosChecker.Out.SendCheckLOS(this, TargetObject, new CheckLOSResponse(CastSpellLosCheckReply));

			return spellCastedFromLosCheck;
		}

		public void CastSpellLosCheckReply(GamePlayer player, ushort response, ushort targetOID)
		{
			if (targetOID == 0)
				return;

			GameObject target = CurrentRegion.GetObject(targetOID);

			if (target == null)
				return;

			if (m_castSpellLosChecks.TryRemove(target, out (Spell, SpellLine, long) value))
			{
				Spell spell = value.Item1;
				SpellLine line = value.Item2;

				if ((response & 0x100) == 0x100 && line != null && spell != null)
				{
					if (target is GameLiving livingTarget && livingTarget.EffectList.GetOfType<NecromancerShadeEffect>() != null)
						target = livingTarget.ControlledBrain?.Body;

					m_spellCastedFromLosCheck = CastSpell(spell, line, target as GameLiving);
				}
				else
				{
					m_spellCastedFromLosCheck = false;
					Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.TargetNotInView));
				}
			}
		}

		#endregion

		#region Styles

		/// <summary>
		/// Styles for this NPC
		/// </summary>
		private IList m_styles = new List<Style>(0);
		public IList Styles
		{
			get { return m_styles; }
			set
			{
				m_styles = value;
				this.SortStyles();
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
				foreach ((Spell, int, int) t in style.Procs)
				{
					if (t.Item1.SpellType == eSpellType.StyleStun && living.HasEffect(t.Item1))
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

				lock (m_lockAbilities)
				{
					tmp = new Dictionary<string, Ability>(m_abilities);
				}

				return tmp;
			}
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
			List<MobXAmbientBehaviour> mxa = (from i in ambientTexts where i.Trigger == trigger.ToString() select i).ToList();
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
					foreach (GamePlayer player in CurrentRegion.GetPlayersInRadius(this, 25000, false))
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

		public override void SetControlledBrain(IControlledBrain controlledBrain)
		{
			if (ControlledBrain == null)
				InitControlledBrainArray(1);

			ControlledBrain = controlledBrain;
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

		/// <summary>
		/// Adds a pet to the current array of pets
		/// </summary>
		/// <param name="controlledNpc">The brain to add to the list</param>
		/// <returns>Whether the pet was added or not</returns>
		public virtual bool AddControlledNpc(IControlledBrain controlledNpc)
		{
			return true;
		}

		/// <summary>
		/// Removes the brain from
		/// </summary>
		/// <param name="controlledNpc">The brain to find and remove</param>
		/// <returns>Whether the pet was removed</returns>
		public virtual bool RemoveControlledNpc(IControlledBrain controlledNpc)
		{
			return true;
		}

		#endregion

		/// <summary>
		/// Whether this NPC is available to add on a fight.
		/// </summary>
		public virtual bool IsAvailable
		{
			get { return !(Brain is IControlledBrain) && !InCombat; }
		}

		/// <summary>
		/// Whether this NPC is aggressive.
		/// </summary>
		public virtual bool IsAggressive
		{
			get
			{
				ABrain brain = Brain;
				return (brain == null) ? false : (brain is IOldAggressiveBrain);
			}
		}

		/// <summary>
		/// Whether this NPC is a friend or not.
		/// </summary>
		/// <param name="npc">The NPC that is checked against.</param>
		/// <returns></returns>
		public virtual bool IsFriend(GameNPC npc)
		{
			if (Faction == null || npc.Faction == null)
				return false;
			return (npc.Faction == Faction || Faction.FriendFactions.Contains(npc.Faction));
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
					lastloot = "";
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
			copyTarget.MaxDistance = MaxDistance;
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
				copyTarget.Spells = new List<Spell>(Spells.Cast<Spell>());

			if (Styles != null && Styles.Count > 0)
				copyTarget.Styles = new ArrayList(Styles);

			if (copyTarget.Inventory != null)
				copyTarget.SwitchWeapon(ActiveWeaponSlot);

			return copyTarget;
		}

		public GameNPC(ABrain defaultBrain) : base()
		{
			if (movementComponent == null)
				movementComponent = (NpcMovementComponent) base.movementComponent;

			Level = 1;
			m_health = MaxHealth;
			Realm = 0;
			m_name = "new mob";
			m_model = 408;
			MaxSpeedBase = 200;
			GuildName = "";
			m_size = 50;
			m_flags = 0;
			m_maxdistance = 0;
			RoamingRange = 0;
			OwnerID = "";
			m_spawnPoint = new Point3D();
			LinkedFactions = new ArrayList(1);

			if (m_ownBrain == null)
			{
				m_ownBrain = defaultBrain;
				m_ownBrain.Body = this;
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

		public virtual double CampBonus { get => m_campBonus; set => m_campBonus = value; }
		public int ScalingFactor { get => scalingFactor; set => scalingFactor = value; }
		public int OrbsReward { get => orbsReward; set => orbsReward = value; }
	}
}
