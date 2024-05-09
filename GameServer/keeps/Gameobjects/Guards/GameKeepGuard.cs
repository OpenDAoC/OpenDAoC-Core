using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// Keep guard is gamemob with just different brain and load from other DB table
	/// </summary>
	public class GameKeepGuard : GameNPC, IKeepItem
	{
		private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Patrol m_Patrol = null;
		public Patrol PatrolGroup
		{
			get { return m_Patrol; }
			set { m_Patrol = value; }
		}

		private string m_templateID = "";
		public string TemplateID
		{
			get { return m_templateID; }
			set { m_templateID = value; }
		}

		private GameKeepComponent m_component;
		public GameKeepComponent Component
		{
			get { return m_component; }
			set { m_component = value; }
		}

		private DbKeepPosition m_position;
		public DbKeepPosition Position
		{
			get { return m_position; }
			set { m_position = value; }
		}

		private GameKeepHookPoint m_hookPoint;
		public GameKeepHookPoint HookPoint
		{
			get { return m_hookPoint; }
			set { m_hookPoint = value; }
		}

		private eRealm m_modelRealm = eRealm.None;
		public eRealm ModelRealm
		{
			get { return m_modelRealm; }
			set { m_modelRealm = value; }
		}

		public override void ProcessDeath(GameObject killer)
		{
			if (killer is GamePlayer p && ConquestService.ConquestManager.IsPlayerNearConquestObjective(p))
			{
				ConquestService.ConquestManager.AddContributors(this.XPGainers.Keys.OfType<GamePlayer>().ToList());
			}

			base.ProcessDeath(killer);
		}

		public bool IsTowerGuard
		{
			get
			{
				if (Component != null && Component.Keep != null)
				{
					return Component.Keep is GameKeepTower;
				}
				return false;
			}
		}

		public bool IsPortalKeepGuard
		{
			get
			{
				if (Component == null || Component.Keep == null)
					return false;
				return Component.Keep.IsPortalKeep;
			}
		}

		/// <summary>
		/// We do this because if we set level when a guard is waiting to respawn,
		/// the guard will never respawn because the guard is given full health and
		/// is then considered alive
		/// </summary>
		public override byte Level
		{
			get
			{
				if (IsPortalKeepGuard)
					return 255;

				return base.Level;
			}
			set
			{
				if (this.IsRespawning)
					m_level = value;
				else
					base.Level = value;
			}
		}

		/// <summary>
		/// Guards always have Mana to cast spells
		/// </summary>
		public override int Mana
		{
			get { return 50000; }
		}

		public override int MaxHealth
		{
			// (base.Level * 4)
			get { return GetModified(eProperty.MaxHealth) + (base.Level * 2); }
		}

		private bool m_changingPositions = false;

		public GameLiving HealTarget = null;

		/// <summary>
		/// The keep lord is under attack, go help them
		/// </summary>
		/// <param name="lord"></param>
		/// <returns>Whether or not we are responding</returns>
		public virtual bool AssistLord(GuardLord lord)
		{
			Follow(lord, StickMinimumRange, int.MaxValue);
			return true;
		}

		#region Combat

		public void GuardStartSpellHealCheckLos(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			if (response is eLosCheckResponse.TRUE && HealTarget != null)
			{
				Spell healSpell = GetGuardHealSmallSpell(Realm);

				if (healSpell != null && !IsStunned && !IsMezzed)
				{
					attackComponent.StopAttack();
					TargetObject = HealTarget;
					CastSpell(healSpell, GuardSpellLine);
				}
			}
		}

		private static Spell GetGuardHealSmallSpell(eRealm realm)
		{
			switch (realm)
			{
				case eRealm.None:
				case eRealm.Albion:
					return GuardSpellDB.AlbGuardHealSmallSpell;
				case eRealm.Midgard:
					return GuardSpellDB.MidGuardHealSmallSpell;
				case eRealm.Hibernia:
					return GuardSpellDB.HibGuardHealSmallSpell;
			}
			return null;
		}

		public void CheckAreaForHeals()
		{
			GameLiving target = null;
			GamePlayer LOSChecker = null;

			foreach (GamePlayer player in GetPlayersInRadius(2000))
			{
				LOSChecker = player;

				if (!player.IsAlive) continue;
				if (GameServer.ServerRules.IsSameRealm(player, this, true))
				{
					if (player.HealthPercent < Properties.KEEP_HEAL_THRESHOLD)
					{
						target = player;
						break;
					}
				}
			}

			if (target == null)
			{
				foreach (GameNPC npc in GetNPCsInRadius(2000))
				{
					if (npc is GameSiegeWeapon) continue;
					if (GameServer.ServerRules.IsSameRealm(npc, this, true))
					{
						if (npc.HealthPercent < Properties.KEEP_HEAL_THRESHOLD)
						{
							target = npc;
							break;
						}
					}
				}
			}

			if (target != null)
			{
				if (LOSChecker == null)
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						LOSChecker = player;
						break;
					}
				}
				if (LOSChecker == null)
					return;
				if (!target.IsAlive) return;

				HealTarget = target;
				LOSChecker.Out.SendCheckLos(this, target, new CheckLosResponse(GuardStartSpellHealCheckLos));
			}
		}

		public void CheckForNuke()
		{
			GameLiving target = TargetObject as GameLiving;
			if (target == null) return;
			if (!target.IsAlive) return;
			if (target is GamePlayer && !GameServer.KeepManager.IsEnemy(this, target as GamePlayer, true)) return;
			if (!IsWithinRadius(target, WorldMgr.VISIBILITY_DISTANCE)) { TargetObject = null; return; }
			GamePlayer LOSChecker = null;
			if (target is GamePlayer) LOSChecker = target as GamePlayer;
			else
			{
				foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					LOSChecker = player;
					break;
				}
			}
			if (LOSChecker == null) return;
			LOSChecker.Out.SendCheckLos(this, target, new CheckLosResponse(GuardStartSpellNukeCheckLos));
		}

		public void GuardStartSpellNukeCheckLos(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			if (response is eLosCheckResponse.TRUE)
			{
				switch (Realm)
				{
					case eRealm.None:
					case eRealm.Albion: LaunchSpell(47, "Pyromancy"); break;
					case eRealm.Midgard: LaunchSpell(48, "Runecarving"); break;
					case eRealm.Hibernia: LaunchSpell(47, "Way of the Eclipse"); break;
				}
			}
		}

		private void LaunchSpell(int spellLevel, string spellLineName)
		{
			if (TargetObject == null)
				return;

			Spell castSpell = null;
			SpellLine castLine = SkillBase.GetSpellLine(spellLineName);
			List<Spell> spells = SkillBase.GetSpellList(castLine.KeyName);

			foreach (Spell spell in spells)
			{
				if (spell.Level == spellLevel)
				{
					castSpell = spell;
					break;
				}
			}

			if (attackComponent.AttackState)
				attackComponent.StopAttack();

			if (IsMoving)
				StopFollowing();

			TurnTo(TargetObject);
			CastSpell(castSpell, castLine);
		}

		/// <summary>
		/// Method to see if the Guard has been left alone long enough to use Ranged attacks
		/// </summary>
		/// <returns></returns>
		public bool CanUseRanged
		{
			get
			{
				if (ObjectState != eObjectState.Active) return false;
				if (this is GuardFighter) return false;
				if (this is GuardArcher || this is GuardLord)
				{
					if (Inventory == null) return false;
					if (Inventory.GetItem(eInventorySlot.DistanceWeapon) == null) return false;
					if (ActiveWeaponSlot == eActiveWeaponSlot.Distance) return false;
				}
				if (this is GuardCaster || this is GuardHealer)
				{
					if (CurrentSpellHandler != null) return false;
				}
				return !BeenAttackedRecently;
			}
		}

		/// <summary>
		/// Because of Spell issues, we will always return this true
		/// </summary>
		/// <param name="target"></param>
		/// <param name="viewangle"></param>
		/// <returns></returns>
		public override bool IsObjectInFront(GameObject target, double viewangle, int alwaysTrueRange = 32)
		{
			return true;
		}

		/// <summary>
		/// Static archers attack with melee the closest if being engaged in melee
		/// </summary>
		/// <param name="ad"></param>
		public override void OnAttackedByEnemy(AttackData ad)
		{
			//this is for static archers only
			if (MaxSpeedBase == 0)
			{
				//if we are currently fighting in melee
				if (ActiveWeaponSlot == eActiveWeaponSlot.Standard || ActiveWeaponSlot == eActiveWeaponSlot.TwoHanded)
				{
					//if we are targeting something, and the distance to the target object is greater than the attack range
					if (TargetObject != null && !IsWithinRadius(TargetObject, attackComponent.AttackRange))
					{
						//stop the attack
						attackComponent.StopAttack();
						//if the distance to the attacker is less than the attack range
						if (IsWithinRadius(ad.Attacker, attackComponent.AttackRange))
						{
							//attack it
							StartAttack(ad.Attacker);
						}
					}
				}
			}
			base.OnAttackedByEnemy(ad);
		}

		/// <summary>
		/// When guards Die and it isnt a keep reset (this killer) we call GuardSpam function
		/// </summary>
		/// <param name="killer"></param>
		public override void Die(GameObject killer)
		{
			if (killer != this)
				GuardSpam(this);
			base.Die(killer);
			if (RespawnInterval == -1)
				Delete();
		}

		#region Guard Spam
		/// <summary>
		/// Sends message to guild for guard death with enemy count in area
		/// </summary>
		/// <param name="guard">The guard object</param>
		public static void GuardSpam(GameKeepGuard guard)
		{
			if (guard.Component == null) return;
			if (guard.Component.Keep == null) return;
			if (guard.Component.Keep.Guild == null) return;

			int inArea = guard.GetEnemyCountInArea();
			string message = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "GameKeepGuard.GuardSpam.Killed", guard.Name, guard.Component.Keep.Name, inArea);
			KeepGuildMgr.SendMessageToGuild(message, guard.Component.Keep.Guild);
		}

		/// <summary>
		/// Gets the count of enemies in the Area
		/// </summary>
		/// <returns></returns>
		public int GetEnemyCountInArea()
		{
			int inArea = 0;
			foreach (GamePlayer NearbyPlayers in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (Component != null)
				{
					if (GameServer.KeepManager.IsEnemy(Component.Keep, NearbyPlayers))
						inArea++;
				}
				else
				{
					if (GameServer.ServerRules.IsAllowedToAttack(this, NearbyPlayers, true))
						inArea++;
				}
			}
			return inArea;
		}


		#endregion

		/// <summary>
		/// Has the NPC been attacked recently.. currently 10 seconds
		/// </summary>
		public bool BeenAttackedRecently
		{
			get
			{
				return GameLoop.GameLoopTime - LastAttackedByEnemyTick < 10 * 1000;
			}
		}
		#endregion

		/// <summary>
		/// When we add a guard to the world, we also attach an AttackFinished handler
		/// We use this to check LOS and range issues for our ranged guards
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			RoamingRange = 0;
			TetherRange = 10000;

			if (!base.AddToWorld())
				return false;

			if (IsPortalKeepGuard && Brain is KeepGuardBrain keepGuardBrain)
			{
				keepGuardBrain.AggroRange = 2000;
				keepGuardBrain.AggroLevel = 99;
			}

			if (PatrolGroup != null && !m_changingPositions)
			{
				bool foundGuard = false;

				foreach (GameKeepGuard guard in PatrolGroup.PatrolGuards)
				{
					if (guard.IsAlive && guard.CurrentWaypoint != null)
					{
						CurrentWaypoint = guard.CurrentWaypoint;
						m_changingPositions = true;
						MoveTo(guard.CurrentRegionID, guard.X - Util.Random(200, 350), guard.Y - Util.Random(200, 350), guard.Z, guard.Heading);
						m_changingPositions = false;
						foundGuard = true;
						break;
					}
				}

				if (!foundGuard)
					CurrentWaypoint = PatrolGroup.PatrolPath;

				MoveOnPath(Patrol.PATROL_SPEED);
			}

			return true;
		}

		/// <summary>
		/// Method to stop a guards respawn
		/// </summary>
		public void StopRespawn()
		{
			if (IsRespawning)
				m_respawnTimer.Stop();
		}

		/// <summary>
		/// When guards respawn we refresh them, if a patrol guard respawns we
		/// call a special function to update leadership
		/// </summary>
		/// <param name="respawnTimer"></param>
		/// <returns></returns>
		protected override int RespawnTimerCallback(ECSGameTimer respawnTimer)
		{
			int temp = base.RespawnTimerCallback(respawnTimer);
			RefreshTemplate();
			return temp;
		}

		/// <summary>
		/// Gets the messages when you click on a guard
		/// </summary>
		/// <param name="player">The player that has done the clicking</param>
		/// <returns></returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
			//You target [Armwoman]
			//You examine the Armswoman. She is friendly and is a realm guard.
			//She has upgraded equipment (5).
			IList list = new ArrayList(4);
			list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.YouTarget", GetName(0, false)));

			if (Realm != eRealm.None)
			{
				list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.YouExamine", GetName(0, false), GetPronoun(0, true), GetAggroLevelString(player, false)));
				if (this.Component != null)
				{
					string text = "";
					if (Component.Keep.Level > 1 && Component.Keep.Level < 250 && GameServer.ServerRules.IsSameRealm(player, this, true))
						text = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.Upgraded", GetPronoun(0, true), Component.Keep.Level);
					if (Properties.USE_KEEP_BALANCING && Component.Keep.Region == 163 && !(Component.Keep is GameKeepTower))
						text += LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.Balancing", GetPronoun(0, true), (Component.Keep.BaseLevel - 50).ToString());
					if (text != "")
						list.Add(text);
				}
			}
			return list;
		}

		/// <summary>
		/// Gets the pronoun for the guards gender
		/// </summary>
		/// <param name="form">Form of the pronoun</param>
		/// <param name="firstLetterUppercase">Weather or not we want the first letter uppercase</param>
		/// <returns></returns>
		public override string GetPronoun(int form, bool firstLetterUppercase)
		{
			string s = "";
			switch (form)
			{
				default:
					{
						// Subjective
						if (Gender == GS.eGender.Male)
							s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.He");
						else s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.She");
						if (!firstLetterUppercase)
							s = s.ToLower();
						break;
					}
				case 1:
					{
						// Possessive
						if (Gender == eGender.Male)
							s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.His");
						else s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.Hers");
						if (!firstLetterUppercase)
							s = s.ToLower();
						break;
					}
				case 2:
					{
						// Objective
						if (Gender == eGender.Male)
							s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.Him");
						else s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.Her");
						if (!firstLetterUppercase)
							s = s.ToLower();
						break;
					}
			}
			return s;
		}
		
		string m_dataObjectID = "";

        #region Database
        /// <summary>
        /// Load the guard from the database
        /// </summary>
        /// <param name="mobobject">The database mobobject</param>
        public override void LoadFromDatabase(DataObject mobobject)
		{
			base.LoadFromDatabase(mobobject);
			foreach (AbstractArea area in this.CurrentAreas)
			{
				if (area is KeepArea)
				{
					AbstractGameKeep keep = (area as KeepArea).Keep;
					Component = new GameKeepComponent();
					Component.Keep = keep;
					m_dataObjectID = mobobject.ObjectId;
					// mob reload command might be reloading guard, so check to make sure it isn't already added
					if (Component.Keep.Guards.ContainsKey(m_dataObjectID) == false)
					{
						Component.Keep.Guards.Add(m_dataObjectID, this);
					}
					break;
				}
			}
			RefreshTemplate();			
		}		

		public void DeleteObject()
		{
			if (Component != null)
			{
				if (Component.Keep != null)
				{
					if (!Component.Keep.Guards.Remove(m_dataObjectID))
					{
						if (log.IsWarnEnabled)
							log.Warn($"Can't find {Position.ClassType} with dataObjectId {m_dataObjectID} in Component InternalID {Component.InternalID} Guard list.");
					}
				}
				else if (log.IsWarnEnabled)
					log.Warn($"Keep is null on delete of guard {Name} with dataObjectId {m_dataObjectID}");

				Component.Delete();
			}
			else if (log.IsWarnEnabled)
				log.Warn($"Component is null on delete of guard {Name} with dataObjectId {m_dataObjectID}");

			HookPoint = null;
			Component = null;
			if (Inventory != null)
				Inventory.ClearInventory();
			Inventory = null;
			Position = null;
			TempProperties.RemoveAllProperties();

			base.Delete();

			SetOwnBrain(null);
			CurrentRegion = null;

			GameEventMgr.RemoveAllHandlersForObject(this);
		}

		public override void Delete()
		{
			if (HookPoint != null && Component != null)
				Component.Keep.Guards.Remove(m_templateID); //Remove(this.ObjectID); LoadFromPosition() uses position.TemplateID as the insertion key

			TempProperties.RemoveAllProperties();

			base.Delete();
		}

		public override void DeleteFromDatabase()
		{
			foreach (AbstractArea area in this.CurrentAreas)
			{
				if (area is KeepArea && Component != null)
				{
					Component.Keep.Guards.Remove(m_dataObjectID); //Remove(this.InternalID); LoadFromDatabase() adds using m_dataObjectID
																		  // break; This is a bad idea.  If there are multiple KeepAreas, we could end up with instantiated keep items that are no longer in the DB
				}
			}
			base.DeleteFromDatabase();
		}

		public void LoadFromPosition(DbKeepPosition pos, GameKeepComponent component)
		{
			m_templateID = pos.TemplateID;
			m_component = component;
			component.Keep.Guards.Add(m_templateID + component.ID, this);
			PositionMgr.LoadGuardPosition(pos, this);
			RefreshTemplate();
			this.AddToWorld();
		}

		/// <summary>
		/// Move a guard to a position
		/// </summary>
		/// <param name="position">The new position for the guard</param>
		public void MoveToPosition(DbKeepPosition position)
		{
			PositionMgr.LoadGuardPosition(position, this);
			if (!InCombat)
				MoveTo(CurrentRegionID, X, Y, Z, Heading);
		}
		#endregion

		/// <summary>
		/// Change guild of guard (emblem on equipment) when keep is claimed
		/// </summary>
		public void ChangeGuild()
		{
			ClothingMgr.EquipGuard(this);

			if (this is GuardMerchant)
			{
				if (IsAlive)
				{
					BroadcastLivingEquipmentUpdate();
				}

				GuildName = "Merchant";
				return;
			}
			else if (this is GuardCurrencyMerchant)
			{
				if (IsAlive)
				{
					BroadcastLivingEquipmentUpdate();
				}

				GuildName = "Orb Merchant";
				return;
			}
			Guild guild = Component.Keep.Guild;
			string guildname = "";
			if (guild != null)
				guildname = guild.Name;

			GuildName = guildname;

			if (Inventory == null)
				return;

			int emblem = 0;
			if (guild != null)
				emblem = guild.Emblem;
			DbInventoryItem lefthand = Inventory.GetItem(eInventorySlot.LeftHandWeapon);
			if (lefthand != null)
				lefthand.Emblem = emblem;

			DbInventoryItem cloak = Inventory.GetItem(eInventorySlot.Cloak);
			if (cloak != null)
			{
				cloak.Emblem = emblem;

				if (cloak.Emblem != 0)
					cloak.Model = 558; // change to a model that looks ok with an emblem

			}
			if (IsAlive)
			{
				BroadcastLivingEquipmentUpdate();
			}
		}

		/// <summary>
		/// Adding special handling for walking to a point for patrol guards to be in a formation
		/// </summary>
		public override void WalkTo(Point3D target, short speed)
		{
			int offX = 0;
			int offY = 0;

			if (IsMovingOnPath && PatrolGroup != null)
				PatrolGroup.GetMovementOffset(this, out offX, out offY);

			base.WalkTo(new Point3D(target.X - offX, target.Y - offY, target.Z), speed);
		}

		public override void ReturnToSpawnPoint(short speed)
		{
			if (PatrolGroup != null)
			{
				attackComponent.StopAttack();
				StopFollowing();

				StandardMobBrain brain = Brain as StandardMobBrain;
				if (brain != null && brain.HasAggro)
				{
					brain.ClearAggroList();
				}

				PatrolGroup.StartPatrol();
				return;
			}

			base.ReturnToSpawnPoint(MaxSpeed);
		}

		public void RefreshTemplate()
		{
			SetRealm();
			SetGuild();
			SetRespawnTime();
			SetGender();
			SetModel();
			SetName();
			SetBlockEvadeParryChance();
			SetBrain();
			SetSpeed();
			SetLevel();
			SetResists();
			AutoSetStats();
			SetAggression();
			ClothingMgr.EquipGuard(this);
			ClothingMgr.SetEmblem(this);
		}

		protected virtual void SetName()
		{
			if (Realm == eRealm.None)
			{
				Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
			}
		}

		protected virtual void SetBlockEvadeParryChance()
		{
			BlockChance = 0;
			EvadeChance = 0;
			ParryChance = 0;
		}

		protected virtual KeepGuardBrain GetBrain() => new KeepGuardBrain();

		protected virtual void SetBrain()
		{
			if (Brain is KeepGuardBrain == false)
			{
				KeepGuardBrain brain = GetBrain();
				AddBrain(brain);
				brain.Body = this;
			}
		}

		protected virtual void SetSpeed()
		{
			if (IsPortalKeepGuard)
			{
				MaxSpeedBase = 575;
			}
			if (Level < 250)
			{
				if (Realm == eRealm.None)
				{
					MaxSpeedBase = 250;
				}
				else if (Level < 50)
				{
					MaxSpeedBase = 270;
				}
				else
				{
					MaxSpeedBase = 350;
				}
			}
			else
			{
				MaxSpeedBase = 575;
			}
		}

		private void SetResists()
		{
			for (int i = (int)eProperty.Resist_First; i <= (int)eProperty.Resist_Last; i++)
			{
				if (this is GuardLord)
				{
					BaseBuffBonusCategory[i] = 40;
				}
				else if (Level < 50)
				{
					BaseBuffBonusCategory[i] = Level / 2 + 1;
				}
				else
				{
					BaseBuffBonusCategory[i] = 26;
				}
			}
		}

		public override void AutoSetStats(DbMob dbMob = null)
		{
			Strength = (short) (Properties.GUARD_AUTOSET_STR_BASE + Level * Properties.GUARD_AUTOSET_STR_MULTIPLIER);
			Constitution = (short) (Properties.GUARD_AUTOSET_CON_BASE + Level * Properties.GUARD_AUTOSET_CON_MULTIPLIER);
			Dexterity = (short) (Properties.GUARD_AUTOSET_DEX_BASE + Level * Properties.GUARD_AUTOSET_DEX_MULTIPLIER);
			Quickness = (short) (Properties.GUARD_AUTOSET_QUI_BASE + Level * Properties.GUARD_AUTOSET_QUI_MULTIPLIER);
			Intelligence = (short) (Properties.GUARD_AUTOSET_INT_BASE + Level * Properties.GUARD_AUTOSET_INT_MULTIPLIER);
		}

		private void SetRealm()
		{
			if (Component != null)
			{
				Realm = Component.Keep.Realm;
			}
			else
			{
				Realm = CurrentZone.Realm;
			}

			if (Realm != eRealm.None)
			{
				ModelRealm = Realm;
			}
			else
			{
				ModelRealm = (eRealm)Util.Random(1, 3);
			}
		}

		private void SetGuild()
		{
			if (Component == null)
			{
				GuildName = "";
			}
			else if (Component.Keep.Guild == null)
			{
				GuildName = "";
			}
			else if ((Component.Keep.Guild == null || Component.Keep.Guild != null)&& this is GuardMerchant)
			{
				GuildName = "Merchant";
			}
			else if ((Component.Keep.Guild == null || Component.Keep.Guild != null) && this is GuardCurrencyMerchant)
			{
				GuildName = "Orb Merchant";
			}
			else
			{
				GuildName = Component.Keep.Guild.Name;
			}
		}

		protected virtual void SetRespawnTime()
		{
			int iVariance = 1000 * Math.Abs(Properties.GUARD_RESPAWN_VARIANCE);
			int iRespawn = 60 * ((Math.Abs(Properties.GUARD_RESPAWN) * 1000) +
				(Util.Random(-iVariance, iVariance)));

			RespawnInterval = (iRespawn > 1000) ? iRespawn : 1000; // Make sure we don't end up with an impossibly low respawn interval.
		}

		protected virtual void SetAggression() { }

		public void SetLevel()
		{
			if (Component != null)
			{
				Component.Keep.SetGuardLevel(this);
			}
		}

		private void SetGender()
		{
			//portal keep guards are always male
			if (IsPortalKeepGuard)
			{
				Gender = eGender.Male;
			}
			else
			{
				if (Util.Chance(50))
				{
					Gender = eGender.Male;
				}
				else
				{
					Gender = eGender.Female;
				}
			}
		}

		protected virtual ICharacterClass GetClass()
        {
			return new DefaultCharacterClass();
		}

		protected virtual void SetModel()
		{
			if (!Properties.AUTOMODEL_GUARDS_LOADED_FROM_DB && !LoadedFromScript)
			{
				return;
			}
			
			var possibleRaces = GetClass().EligibleRaces.FindAll(s => s.GetModel(Gender) != eLivingModel.None);
			if (possibleRaces.Count > 0)
			{
				var indexPick = Util.Random(0, possibleRaces.Count - 1);
				Model = (ushort)possibleRaces[indexPick].GetModel(Gender);
			}
		}

		private static SpellLine GuardSpellLine { get; } = new SpellLine("GuardSpellLine", "Guard Spells", "unknown", false);
	}

	public class GuardSpellDB
    {
		private static Spell m_albLordHealSpell;
		private static Spell m_midLordHealSpell;
		private static Spell m_hibLordHealSpell;

		private static DbSpell BaseHealSpell
        {
			get
            {
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.Name = "Guard Heal";
				spell.Range = WorldMgr.VISIBILITY_DISTANCE;
				spell.Type = eSpellType.Heal.ToString();
				return spell;
			}
        }

		private static DbSpell LordBaseHealSpell
		{
			get
			{
				DbSpell spell = BaseHealSpell;
				spell.CastTime = 2;
				spell.Target = "Self";
				spell.Value = 225;
				if (GameServer.Instance.Configuration.ServerType != EGameServerType.GST_PvE)
					spell.Uninterruptible = true;
				return spell;
			}
		}

		private static DbSpell GuardBaseHealSpell
		{
			get
			{
				DbSpell spell = BaseHealSpell;
				spell.CastTime = 2;
				spell.Value = 200;
				spell.Target = "Realm";
				return spell;
			}
		}

		public static Spell AlbLordHealSpell
		{
			get
			{
				if (m_albLordHealSpell == null)
				{
					DbSpell spell = LordBaseHealSpell;
					spell.ClientEffect = 1340;
					spell.SpellID = 90001;
					m_albLordHealSpell = new Spell(spell, 50);
				}
				return m_albLordHealSpell;
			}
		}

		public static Spell MidLordHealSpell
		{
			get
			{
				if (m_midLordHealSpell == null)
				{
					DbSpell spell = LordBaseHealSpell;
					spell.ClientEffect = 3011;
					spell.SpellID = 90002;
					m_midLordHealSpell = new Spell(spell, 50);
				}
				return m_midLordHealSpell;
			}
		}

		public static Spell HibLordHealSpell
		{
			get
			{
				if (m_hibLordHealSpell == null)
				{
					DbSpell spell = LordBaseHealSpell;
					spell.ClientEffect = 3030;
					spell.SpellID = 90003;
					m_hibLordHealSpell = new Spell(spell, 50);
				}
				return m_hibLordHealSpell;
			}
		}

		private static Spell m_albGuardHealSmallSpell;
		private static Spell m_midGuardHealSmallSpell;
		private static Spell m_hibGuardHealSmallSpell;

		public static Spell AlbGuardHealSmallSpell
		{
			get
			{
				if (m_albGuardHealSmallSpell == null)
				{
					DbSpell spell = GuardBaseHealSpell;
					spell.ClientEffect = 1340;
					spell.SpellID = 90004;
					m_albGuardHealSmallSpell = new Spell(spell, 50);
				}
				return m_albGuardHealSmallSpell;
			}
		}

		public static Spell MidGuardHealSmallSpell
		{
			get
			{
				if (m_midGuardHealSmallSpell == null)
				{
					DbSpell spell = GuardBaseHealSpell;
					spell.ClientEffect = 3011;
					spell.SpellID = 90005;
					m_midGuardHealSmallSpell = new Spell(spell, 50);
				}
				return m_midGuardHealSmallSpell;
			}
		}

		public static Spell HibGuardHealSmallSpell
		{
			get
			{
				if (m_hibGuardHealSmallSpell == null)
				{
					DbSpell spell = GuardBaseHealSpell;
					spell.ClientEffect = 3030;
					spell.SpellID = 90006;
					m_hibGuardHealSmallSpell = new Spell(spell, 50);
				}
				return m_hibGuardHealSmallSpell;
			}
		}
	}
}
