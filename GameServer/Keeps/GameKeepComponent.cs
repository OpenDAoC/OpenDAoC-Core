using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Keeps
{
	//TODO : find all skin of keep door to load it from here
	/// <summary>
	/// A keepComponent
	/// </summary>
	public class GameKeepComponent : GameLiving, IComparable, IGameKeepComponent
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected readonly ushort INVISIBLE_MODEL = 150;

		public enum eComponentSkin : byte
		{
			Gate = 0,
			WallInclined = 1,
			WallInclined2 = 2,
			WallAngle2 = 3,
			TowerAngle = 4,
			WallAngle = 5,
			WallAngleInternal = 6,
			TowerHalf = 7,
			WallHalfAngle = 8,
			Wall = 9,
			Keep = 10,
			Tower = 11,
			WallWithDoorLow = 12,
			WallWithDoorHigh = 13,
			BridgeHigh = 14,
			WallInclinedLow = 15,
			BridgeLow = 16,
			BridgeHightSolid = 17,
			BridgeHighWithHook = 18,
			GateFree = 19,
			BridgeHightWithHook2 = 20,

			NewSkinClimbingWall = 27,
			NewSkinKeep = 30,
			NewSkinTower = 31,
		}

		#region properties
		[Obsolete("Use Keep instead")]
		public AbstractGameKeep AbstractKeep
		{
			get { return Keep; }
			set { Keep = value; }
		}

		public AbstractGameKeep Keep { get; set; }

		public override eGameObjectType GameObjectType => eGameObjectType.KEEP_COMPONENT;

		public int ID { get; set; }

		public int Height => Keep.Height;

		public int Skin { get; set; }

		public bool Climbing
		{
			get
			{
				if (Properties.ALLOW_TOWER_CLIMB)
				{
					if (Skin == (int)eComponentSkin.Wall || Skin == (int)eComponentSkin.NewSkinClimbingWall || Skin == (int)eComponentSkin.Tower || Skin == (int)eComponentSkin.NewSkinTower && !Keep.IsPortalKeep) return true;
				}
				else
				{
					if (Skin == (int)eComponentSkin.Wall || Skin == (int)eComponentSkin.NewSkinClimbingWall && !Keep.IsPortalKeep) return true;
				}
				return false;
			}
		}

		/// <summary>
		/// relative X to keep
		/// </summary>
		public int ComponentX { get; set; }

		/// <summary>
		/// relative Y to keep
		/// </summary>
		public int ComponentY { get; set; }

		/// <summary>
		/// relative heading to keep ( 0, 1, 2, 3)
		/// </summary>
		public int ComponentHeading { get; set; }

		protected int m_oldMaxHealth;

		public override byte Level => (byte)(Keep.BaseLevel-10 + (Keep.Level * 3));

		public override eRealm Realm
		{
			get
			{
				if (Keep != null) return Keep.Realm;
				return eRealm.None;
			}
		}

		protected byte m_oldHealthPercent;
		protected bool m_isRaized;

		public Dictionary<int, GameKeepHookPoint> HookPoints { get; set; }

		public Hashtable Positions { get; }

		protected string m_CreateInfo = "";
		#endregion

		public override int RealmPointsValue => 0;

		public override long ExperienceValue => 0;

		public override int AttackRange => 1000;

		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list = base.GetExamineMessages(player);

			if (player.Client.Account.PrivLevel > 1)
			{
				list.Add(Name + " with a Z of " + Z.ToString());
			}

			return list;
		}

		/// <summary>
		/// Procs don't normally fire on game keep components
		/// </summary>
		public override bool AllowWeaponMagicalEffect(AttackData ad, DbInventoryItem weapon, Spell weaponSpell)
		{
			if (weapon.Flags == 10) //Bruiser or any other item needs Itemtemplate "Flags" set to 10 to proc on keep components
				return true;
			else return false;
		}

		/// <summary>
		/// do not regen
		/// </summary>
		public override void StartHealthRegeneration()
		{
			m_repairTimer = new ECSGameTimer(this);
			m_repairTimer.Callback = new ECSGameTimer.ECSTimerCallback(RepairTimerCallback);
			m_repairTimer.Interval = repairInterval;
			m_repairTimer.Start(1);
		}

		public virtual void RemoveTimers()
		{
			if (m_repairTimer != null)
			{
				m_repairTimer.Stop();
				m_repairTimer = null;
			}
		}

		public GameKeepComponent()
		{
			HookPoints = new Dictionary<int, GameKeepHookPoint>(41);
			Positions = new Hashtable();
		}

		/// <summary>
		/// load component from db object
		/// </summary>
		public virtual void LoadFromDatabase(DbKeepComponent component, AbstractGameKeep keep)
		{
			Region myregion = WorldMgr.GetRegion((ushort)keep.Region);
			if (myregion == null)
				return;
			Keep = keep;
			//this.DBKeepComponent = component;
			base.LoadFromDatabase(component);
			//this x and y is for get object in radius
			double angle = keep.Heading * ((Math.PI * 2) / 360); // angle*2pi/360;
			X = (int)(keep.X + ((sbyte)component.X * 148 * Math.Cos(angle) + (sbyte)component.Y * 148 * Math.Sin(angle)));
			Y = (int)(keep.Y - ((sbyte)component.Y * 148 * Math.Cos(angle) - (sbyte)component.X * 148 * Math.Sin(angle)));
			Z = keep.Z;
			// and this one for packet sent
			ComponentX = component.X;
			ComponentY = component.Y;
			ComponentHeading = (ushort)component.Heading;
			//need check to be sure for heading
			angle = (component.Heading * 90 + keep.Heading);
			if (angle > 360) angle -= 360;
			Heading = (ushort)(angle / 0.08789);
			Name = keep.Name;
			Model = INVISIBLE_MODEL;
			Skin = component.Skin;
			m_oldMaxHealth = MaxHealth;
			Health = MaxHealth;
			//			this.Health = component.Health;
			m_oldHealthPercent = HealthPercent;
			CurrentRegion = myregion;
			ID = component.ID;
			SaveInDB = false;
			IsRaized = false;
			LoadPositions();
			AddToWorld();
			FillPositions();
			RepairedHealth = MaxHealth;
			m_CreateInfo = component.CreateInfo;
			StartHealthRegeneration();
		}

		public virtual void LoadPositions()
		{
			ushort region = CurrentRegionID;
			if (CurrentRegion is BaseInstance)
			{
				region = (CurrentRegion as BaseInstance).Skin;
			}

			DbBattleground bg = GameServer.KeepManager.GetBattleground(region);

			this.Positions.Clear();

			var whereClause = DB.Column("ComponentSkin").IsEqualTo(Skin);
			if (Skin != (int)eComponentSkin.Keep && Skin != (int)eComponentSkin.Tower && Skin != (int)eComponentSkin.Gate)
			{
				whereClause = whereClause.And(DB.Column("ComponentRotation").IsEqualTo(ComponentHeading));
			}
			if (bg != null && GameServer.Instance.Configuration.ServerType != EGameServerType.GST_PvE)
			{
				// Battlegrounds, ignore all but GameKeepDoor
				whereClause = whereClause.And(DB.Column("ClassType").IsEqualTo("DOL.GS.Keeps.GameKeepDoor"));
			}
			var DBPositions = DOLDB<DbKeepPosition>.SelectObjects(whereClause);

			foreach (DbKeepPosition position in DBPositions)
			{
				DbKeepPosition[] list = this.Positions[position.TemplateID] as DbKeepPosition[];
				if (list == null)
				{
					list = new DbKeepPosition[4];
					this.Positions[position.TemplateID] = list;
				}

				list[position.Height] = position;
			}
		}

		/// <summary>
		/// Populate GameKeepItems for this component into the keep
		/// </summary>
		public virtual void FillPositions()
		{
			foreach (DbKeepPosition[] positionGroup in Positions.Values)
			{
				for (int i = this.Height; i >= 0; i--)
				{
					if (positionGroup[i] is DbKeepPosition position)
					{
						bool create = false;
						string sKey = position.TemplateID + ID;

						switch (position.ClassType)
						{
							case "DOL.GS.Keeps.GameKeepBanner":
								if (Keep.Banners.ContainsKey(sKey) == false)
									create = true;
								break;
							case "DOL.GS.Keeps.GameKeepDoor":
								if (Keep.Doors.ContainsKey(sKey) == false)
									create = true;
								break;
							case "DOL.GS.Keeps.FrontierTeleportStone":
								if (Keep.TeleportStone == null)
									create = true;
								break;
							case "DOL.GS.Keeps.Patrol":
								if ((position.KeepType == (int)AbstractGameKeep.eKeepType.Any || position.KeepType == (int)Keep.KeepType)
									&& Keep.Patrols.ContainsKey(sKey) == false)
								{
									Patrol p = new Patrol(this);
									p.SpawnPosition = position;
									p.PatrolID = position.TemplateID;
									p.InitialiseGuards();
								}
								continue;
							case "DOL.GS.Keeps.FrontierHastener":
								if (Keep.HasHastener && log.IsWarnEnabled)
									log.Warn($"FillPositions(): KeepComponent_ID {InternalID}, KeepPosition_ID {position.ObjectId}: There is already a {position.ClassType} on Keep {Keep.KeepID}");

								if (Keep.Guards.ContainsKey(sKey) == false)
								{
									Keep.HasHastener = true;
									create = true;
								}
								break;
							case "DOL.GS.Keeps.MissionMaster":
								if (Keep.HasCommander && log.IsWarnEnabled)
									log.Warn($"FillPositions(): KeepComponent_ID {InternalID}, KeepPosition_ID {position.ObjectId}: There is already a {position.ClassType} on Keep {Keep.KeepID}");

								if (Keep.Guards.ContainsKey(sKey) == false)
								{
									Keep.HasCommander = true;
									create = true;
								}
								break;
							case "DOL.GS.Keeps.GuardLord":
								if (Keep.HasLord && log.IsWarnEnabled)
									log.Warn($"FillPositions(): KeepComponent_ID {InternalID}, KeepPosition_ID {position.ObjectId}: There is already a {position.ClassType} on Keep {Keep.KeepID}");

								if (Keep.Guards.ContainsKey(sKey) == false)
								{
									Keep.HasLord = true;
									create = true;
								}
								break;
							default:
								if (Keep.Guards.ContainsKey(sKey) == false)
									create = true;
								break;
						}// switch (position.ClassType)

						if (create)
						{
							//create the object
							try
							{
								Assembly asm = Assembly.GetExecutingAssembly();
								IKeepItem obj = (IKeepItem)asm.CreateInstance(position.ClassType, true);
								if (obj != null)
									obj.LoadFromPosition(position, this);

								if (ServerProperties.Properties.ENABLE_DEBUG)
								{
									if (obj is GameLiving living)
										living.Name += " is living, component " + obj.Component.ID;
									else if (obj is GameObject game)
										game.Name += " is object, component " + obj.Component.ID;
								}
							}
							catch (Exception ex)
							{
								log.Error("FillPositions(): " + position.ClassType, ex);
							}
						}
						else
						{
							/* Why move the object?  We should notify the server admin of the duplicate and let them figure out what is causing it in their DB.
							* Otherwise, we're assuming the former position/component combination wasn't valid, and that's an error that should be reported in any case.						
							//move the object
							switch (position.ClassType)
							{
								case "DOL.GS.Keeps.GameKeepBanner":
									if (this.AbstractKeep.Banners[position.TemplateID] is IKeepItem banner && banner.Position != position)
										banner.MoveToPosition(position);
									break;
								case "DOL.GS.Keeps.GameKeepDoor":
								case "DOL.GS.Keeps.FrontierPortalStone":
									break;  // these dont move
								default:
									if (this.AbstractKeep.Guards[position.TemplateID] is IKeepItem guard)
										guard.MoveToPosition(position);
									break;
							}*/
							if (log.IsWarnEnabled)
								log.Warn($"FillPositions(): Keep {Keep.KeepID} already has a {position.ClassType} assigned to Position {position.ObjectId} on Component {InternalID}");
					}
						break; // We found the highest item for that position, move onto the next one
					}// if (positionGroup[i] is DBKeepPosition position)
				}// for (int i = this.Height; i >= 0; i--)
			}// foreach (DBKeepPosition[] positionGroup in this.Positions.Values)

			foreach (var guard in Keep.Guards.Values)
			{
				if (guard.PatrolGroup != null)
					continue;
				if (guard.HookPoint != null) continue;
				if (guard.Position == null) continue;
				if (guard.Position.Height > guard.Component.Height)
					guard.RemoveFromWorld();
				else
				{
					if (guard.Position.Height <= guard.Component.Height &&
						guard.ObjectState != GameObject.eObjectState.Active && !guard.IsRespawning)
						guard.AddToWorld();
				}
			}

			foreach (var banner in Keep.Banners.Values)
			{
				if (banner.Position == null) continue;
				if (banner.Position.Height > banner.Component.Height)
					banner.RemoveFromWorld();
				else
				{
					if (banner.Position.Height <= banner.Component.Height &&
						banner.ObjectState != GameObject.eObjectState.Active)
						banner.AddToWorld();
				}
			}
		}

		/// <summary>
		/// save component in DB
		/// </summary>
		public override void SaveIntoDatabase()
		{
			DbKeepComponent obj = null;
			bool New = false;
			if (InternalID != null)
				obj = GameServer.Database.FindObjectByKey<DbKeepComponent>(InternalID);
			if (obj == null)
			{
				obj = new DbKeepComponent();
				New = true;
			}
			obj.KeepID = Keep.KeepID;
			obj.Heading = ComponentHeading;
			obj.Health = Health;
			obj.X = ComponentX;
			obj.Y = ComponentY;
			obj.ID = ID;
			obj.Skin = Skin;
			obj.CreateInfo = m_CreateInfo;

			if (New)
			{
				GameServer.Database.AddObject(obj);
				InternalID = obj.ObjectId;
				log.DebugFormat("Added new component {0} for keep ID {1}, skin {2}, health {3}", ID, Keep.KeepID, Skin, Health);
			}
			else
			{
				GameServer.Database.SaveObject(obj);
			}
			base.SaveIntoDatabase();
		}

		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (damageAmount > 0)
			{
				Keep.LastAttackedByEnemyTick = CurrentRegion.Time;
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);

				//only on hp change
				if (m_oldHealthPercent != HealthPercent)
				{
					m_oldHealthPercent = HealthPercent;

					foreach (GamePlayer player in ClientService.GetPlayersOfRegion(CurrentRegion))
					{
						ClientService.UpdateObjectForPlayer(player, this);
						player.Out.SendKeepComponentDetailUpdate(this); // I know this works, not sure if ObjectUpdate is needed - Tolakram
					}
				}
			}
		}

		public override void ModifyAttack(AttackData attackData)
		{
			// Allow a GM to use commands to damage components, regardless of toughness setting
			if (attackData.DamageType == eDamageType.GM)
				return;

			int toughness = Properties.SET_STRUCTURES_TOUGHNESS;
			int baseDamage = attackData.Damage;
			int styleDamage = attackData.StyleDamage;
			int criticalDamage = 0;

			GameLiving source = attackData.Attacker;

			if (source is GamePlayer)
			{
				baseDamage = (baseDamage - (baseDamage * 5 * Keep.Level / 100)) * toughness / 100;
				styleDamage = (styleDamage - (styleDamage * 5 * Keep.Level / 100)) * toughness / 100;
			}
			else if (source is GameNPC)
			{
				if (!Properties.STRUCTURES_ALLOWPETATTACK)
				{
					baseDamage = 0;
					styleDamage = 0;
					attackData.AttackResult = eAttackResult.NotAllowed_ServerRules;
				}
				else
				{
					baseDamage = (baseDamage - (baseDamage * 5 * Keep.Level / 100)) * toughness / 100;
					styleDamage = (styleDamage - (styleDamage * 5 * Keep.Level / 100)) * toughness / 100;

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

		public override void Die(GameObject killer)
		{
			base.Die(killer);
			if (Keep is GameKeepTower && Properties.CLIENT_VERSION_MIN >= (int)GameClient.eClientVersion.Version175)
			{
				if (IsRaized == false)
				{
					Notify(KeepEvent.TowerRaized, Keep, new KeepEventArgs(Keep, killer.Realm));
					PlayerMgr.BroadcastRaize(Keep, killer.Realm);
					IsRaized = true;

					foreach (var guard in Keep.Guards.Values)
					{
						guard.MoveTo(guard.CurrentRegionID, guard.X, guard.Y, Keep.Z, guard.Heading);
						guard.SpawnPoint.Z = Keep.Z;
					}
				}
			}

			foreach (GamePlayer player in ClientService.GetPlayersOfRegion(CurrentRegion))
				player.Out.SendKeepComponentDetailUpdate(this);
		}

		public override void Delete()
		{
			StopHealthRegeneration();
			RemoveTimers();
			HookPoints.Clear();
			Positions.Clear();
			Keep = null;
			base.Delete();
			CurrentRegion = null;
		}

		/// <summary>
		/// Remove a component and delete it from the database
		/// </summary>
		public virtual void Remove()
		{
			Delete();
			DbKeepComponent obj = null;
			if (this.InternalID != null)
				obj = GameServer.Database.FindObjectByKey<DbKeepComponent>(this.InternalID);
			if (obj != null)
				GameServer.Database.DeleteObject(obj);

			log.Warn("Keep Component deleted from database: " + obj.ID);
			//todo find a packet to remove the keep
		}

		/// <summary>
		/// IComparable.CompareTo implementation.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is GameKeepComponent)
				return (ID - ((GameKeepComponent)obj).ID);
			else
				return 0;
		}

		public virtual byte Status
		{
			get
			{
				if (Keep is GameKeepTower)
				{
					if (m_isRaized)
					{
						if (HealthPercent >= 25)
						{
							IsRaized = false;
						}
						else return 0x02;
					}
					if (HealthPercent < 35) return 0x01;//broken
				}
				if (Keep is GameKeep)
					if (!IsAlive) return 0x01;//broken

				return 0x00;

			}
		}

		public virtual void UpdateLevel()
		{
			if ((IsRaized == false) && (MaxHealth != m_oldMaxHealth))
			{
				if (m_oldMaxHealth > 0)
				{
					Health = (int)Math.Ceiling(((double)Health) * ((double)MaxHealth) / ((double)m_oldMaxHealth));
				}
				else
				{
					Health = MaxHealth;
				}
				m_oldMaxHealth = MaxHealth;
			}
		}

		public virtual bool IsRaized
		{
			get { return m_isRaized; }
			set
			{
				RepairedHealth = 0;
				m_isRaized = value;
				if (value == true)
				{
					if (Keep.Level > 1)
						Keep.ChangeLevel(1);
				}
				else
				{
					FillPositions();
				}
			}
		}

		public int RepairedHealth = 0;

		protected ECSGameTimer m_repairTimer;
		protected static int repairInterval = 30 * 60 * 1000;

		public virtual int RepairTimerCallback(ECSGameTimer timer)
		{
			if (HealthPercent == 100 || Keep.InCombat)
				return repairInterval;

			Repair((MaxHealth / 100) * 5);
			return repairInterval;
		}

		public virtual void Repair(int amount)
		{
			if (amount > 0)
			{
				byte oldStatus = Status;
				Health += amount;
				m_oldHealthPercent = HealthPercent;
				if (oldStatus != Status)
				{
					foreach (GamePlayer player in ClientService.GetPlayersOfRegion(CurrentRegion))
						player.Out.SendKeepComponentDetailUpdate(this);
				}

				//if a tower is repaired reload the guards so they arent on the floor
				if (Keep is GameKeepTower && oldStatus == 0x02 && oldStatus != Status)
				{
					foreach (GameKeepComponent component in Keep.KeepComponents)
						component.FillPositions();
				}

				RepairedHealth = Health;
			}
		}

		public override string ToString()
		{
			if (Keep == null)
			{
				return "Keep is null!";
			}

			return new StringBuilder(base.ToString())
				.Append(" ComponentID=").Append(ID)
				.Append(" Skin=").Append(Skin)
				.Append(" Height=").Append(Height)
				.Append(" Heading=").Append(Heading)
				.Append(" nComponentX=").Append((sbyte)ComponentX)
				.Append(" ComponentY=").Append((sbyte)ComponentY)
				.Append(" ComponentHeading=").Append(ComponentHeading)
				.ToString();
		}
	}
}
