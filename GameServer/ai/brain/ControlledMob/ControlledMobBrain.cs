using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using log4net;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A brain that can be controlled
	/// </summary>
	public class ControlledMobBrain : StandardMobBrain, IControlledBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public const int MAX_PET_AGGRO_DISTANCE = 512; // Tolakram - Live test with caby pet - I was extremely close before auto aggro
		// note that a minimum distance is inforced in GameNPC
		public const short MIN_OWNER_FOLLOW_DIST = 50;
		//4000 - rough guess, needs to be confirmed
		public const short MAX_OWNER_FOLLOW_DIST = 10000; // setting this to max stick distance

		protected int m_tempX = 0;
		protected int m_tempY = 0;
		protected int m_tempZ = 0;

		/// <summary>
		/// Holds the controlling player of this brain
		/// </summary>
		protected readonly GameLiving m_owner;

		/// <summary>
		/// Holds the walk state of the brain
		/// </summary>
		protected eWalkState m_walkState;

		/// <summary>
		/// Holds the aggression level of the brain
		/// </summary>
		protected eAggressionState m_aggressionState;

		private HashSet<GameLiving> _buffedTargets = new();
		private readonly Lock _buffedTargetsLock = new();

		/// <summary>
		/// Constructs new controlled npc brain
		/// </summary>
		/// <param name="owner"></param>
		public ControlledMobBrain(GameLiving owner) : base()
		{
			m_owner = owner ?? throw new ArgumentNullException("owner");
			m_aggressionState = eAggressionState.Defensive;
			m_walkState = eWalkState.Follow;

			if (owner is GameNPC npcOwner && npcOwner.Brain is StandardMobBrain npcOwnerBrain)
				AggroLevel = npcOwnerBrain.AggroLevel;
			else
				AggroLevel = 99;
			AggroRange = MAX_PET_AGGRO_DISTANCE;

			FSM.ClearStates();
			FSM.Add(new ControlledMobState_WAKING_UP(this));
			FSM.Add(new ControlledMobState_DEFENSIVE(this));
			FSM.Add(new ControlledMobState_AGGRO(this));
			FSM.Add(new ControlledMobState_PASSIVE(this));
		}

		protected bool m_isMainPet = true;

		public override int AggroRange => Math.Min(base.AggroRange, MAX_PET_AGGRO_DISTANCE);

		/// <summary>
		/// Checks if this NPC is a permanent/charmed or timed pet
		/// </summary>
		public bool IsMainPet
		{
			get { return m_isMainPet; }
			set { m_isMainPet = value; }
		}

		/// <summary>
		/// The interval for thinking, set via server property, default is 1500 or every 1.5 seconds
		/// </summary>
		public override int ThinkInterval
		{
			get { return GS.ServerProperties.Properties.PET_THINK_INTERVAL; }
		}

		protected override int ThinkOffsetOnStart => 0;

		#region Control

		/// <summary>
		/// Gets the controlling owner of the brain
		/// </summary>
		public GameLiving Owner
		{
			get { return m_owner; }
		}

        /// <summary>
        /// Find the player owner of the pets at the top of the tree
        /// </summary>
        /// <returns>Player owner at the top of the tree.  If there was no player, then return null.</returns>
        public virtual GamePlayer GetPlayerOwner()
        {
            GameLiving owner = Owner;
            int i = 0;
            while (owner is GameNPC && owner != null)
            {
                i++;
                if (i > 50)
                    throw new Exception("GetPlayerOwner() from " + Owner.Name + "caused a cyclical loop.");
                //If this is a pet, get its owner
                if (((GameNPC)owner).Brain is IControlledBrain)
                    owner = ((IControlledBrain)((GameNPC)owner).Brain).Owner;
                //This isn't a pet, that means it's at the top of the tree.  This case will only happen if
                //owner is not a GamePlayer
                else
                    break;
            }
            //Return if we found the gameplayer
            if (owner is GamePlayer)
                return (GamePlayer)owner;
            //If the root owner was not a player or npc then make sure we know that something went wrong!
            if (!(owner is GameNPC))
                throw new Exception("Unrecognized owner: " + owner.GetType().FullName);
            //No GamePlayer at the top of the tree
            return null;
        }

        public virtual GameNPC GetNPCOwner()
        {
            if (!(Owner is GameNPC))
                return null;

            GameNPC owner = Owner as GameNPC;

            int i = 0;
            while (owner != null)
            {
                i++;
                if (i > 50)
                {
                    log.Error("Boucle itérative dans GetNPCOwner !");
                    break;
                }
                if (owner.Brain is IControlledBrain)
                {
                    if ((owner.Brain as IControlledBrain).Owner is GamePlayer)
                        return null;
                    else
                        owner = (owner.Brain as IControlledBrain).Owner as GameNPC;
                }
                else
                    break;
            }
            return owner;
        }

        public virtual GameLiving GetLivingOwner()
        {
            GamePlayer player = GetPlayerOwner();
            if (player != null)
                return player;

            GameNPC npc = GetNPCOwner();
            if (npc != null)
                return npc;

            return null;
        }

        /// <summary>
        /// Gets or sets the walk state of the brain
        /// </summary>
        public virtual eWalkState WalkState
        {
            get => m_walkState;
            set
            {
                if (m_walkState != value)
                    Body?.effectListComponent.RequestPlayerUpdate(EffectService.PlayerUpdate.ICONS);

                m_walkState = value;
            }
        }

        /// <summary>
        /// Gets or sets the aggression state of the brain
        /// </summary>
        public virtual eAggressionState AggressionState
        {
            get => m_aggressionState;
            set
            {
                if (m_aggressionState != value)
                    Body?.effectListComponent.RequestPlayerUpdate(EffectService.PlayerUpdate.ICONS);

                m_aggressionState = value;

                if (m_aggressionState is eAggressionState.Passive)
                {
                    Disengage();

                    if (WalkState == eWalkState.Follow)
                        FollowOwner();
                    else if (m_tempX > 0 && m_tempY > 0 && m_tempZ > 0)
                    {
                        Body.StopFollowing();
                        Body.WalkTo(new Point3D(m_tempX, m_tempY, m_tempZ), Body.MaxSpeed);
                    }
                }
            }
        }

        /// <summary>
        /// Attack the target on command
        /// </summary>
        /// <param name="target"></param>
        public virtual void Attack(GameObject target)
		{
			if (AggressionState is eAggressionState.Passive)
				AggressionState = eAggressionState.Defensive;

			if (m_orderAttackTarget == target)
				return;

			m_orderAttackTarget = target as GameLiving;
			FSM.SetCurrentState(eFSMStateType.AGGRO);

			if (target != Body.TargetObject && Body.IsCasting)
				Body.StopCurrentSpellcast();

			AttackMostWanted();
		}

		public virtual void CheckAggressionStateOnPlayerOrder()
		{
			// We switch to defensive mode if we're in aggressive and have a target, so that we don't immediately aggro back
			if (AggressionState is eAggressionState.Aggressive && Body.TargetObject != null)
				AggressionState = eAggressionState.Defensive;
		}

		public virtual void Disengage()
		{
			m_orderAttackTarget = null;
			ClearAggroList();
			Body.StopAttack();
			Body.StopCurrentSpellcast();
			Body.TargetObject = null;
		}

		/// <summary>
		/// Follow the target on command
		/// </summary>
		/// <param name="target"></param>
		public virtual void Follow(GameObject target)
		{
			WalkState = eWalkState.Follow;
			Body.Follow(target, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
		}

		/// <summary>
		/// Stay at current position on command
		/// </summary>
		public virtual void Stay()
		{
			m_tempX = Body.X;
			m_tempY = Body.Y;
			m_tempZ = Body.Z;
			WalkState = eWalkState.Stay;
			Body.StopMoving();
		}

		/// <summary>
		/// Go to owner on command
		/// </summary>
		public virtual void ComeHere()
		{
			m_tempX = Owner.X;
			m_tempY = Owner.Y;
			m_tempZ = Owner.Z;
			WalkState = eWalkState.ComeHere;
			Body.StopFollowing();
			Body.PathTo(Owner, Body.MaxSpeed);
		}

		/// <summary>
		/// Go to targets location on command
		/// </summary>
		/// <param name="target"></param>
		public virtual void Goto(GameObject target)
		{
			m_tempX = target.X;
			m_tempY = target.Y;
			m_tempZ = target.Z;
			WalkState = eWalkState.GoTarget;
			Body.StopFollowing();
			Body.PathTo(target, Body.MaxSpeed);
		}

		public virtual void SetAggressionState(eAggressionState state)
		{
			AggressionState = state;
		}

		/// <summary>
		/// Updates the pet window
		/// </summary>
		public virtual void UpdatePetWindow()
		{
			(m_owner as GamePlayer)?.Out.SendPetWindow(Body, ePetWindowAction.Update, m_aggressionState, m_walkState);
		}

		/// <summary>
		/// Start following the owner
		/// </summary>
		public virtual void FollowOwner()
		{
			if (Body.IsAttacking)
				Disengage();

			if (Owner is GamePlayer
			    && IsMainPet
			    && ((GamePlayer)Owner).CharacterClass.ID != (int)eCharacterClass.Animist
			    && ((GamePlayer)Owner).CharacterClass.ID != (int)eCharacterClass.Theurgist)
				Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
			else if (Owner is GameNPC)
				Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
		}

		#endregion

		#region AI

		/// <summary>
		/// The attack target ordered by the owner
		/// </summary>
		protected GameLiving m_orderAttackTarget;

		public GameLiving OrderedAttackTarget {
			get { return m_orderAttackTarget; }
			set { m_orderAttackTarget = value; }
        }

		/// <summary>
		/// Starts the brain thinking and resets the inactivity countdown
		/// </summary>
		/// <returns>true if started</returns>
		public override bool Start()
		{
			if (!base.Start())
				return false;

			if (WalkState == eWalkState.Follow)
				FollowOwner();

			return true;
		}

		public override bool Stop()
		{
			if (!base.Stop())
				return false;

			OnRelease();
			return true;
		}

		/// <summary>
		/// Checks the Abilities
		/// </summary>
		public override void CheckAbilities()
		{
			if (Body.Abilities == null || Body.Abilities.Count <= 0)
				return;

			foreach (Ability ab in Body.Abilities.Values)
			{
				switch (ab.KeyName)
				{
					case Abilities.Intercept:
					{
						GamePlayer playerOwner = GetPlayerOwner();

						if (playerOwner != null)
						{
							InterceptAbilityHandler.CheckExistingEffectsOnTarget(Body, playerOwner, false, out bool foundOurEffect, out InterceptECSGameEffect existingEffectFromAnotherSource);

							if (foundOurEffect)
								break;

							if (existingEffectFromAnotherSource != null)
								EffectService.RequestImmediateCancelEffect(existingEffectFromAnotherSource);

							new InterceptECSGameEffect(new ECSGameEffectInitParams(Body, 0, 1), Body, playerOwner);
						}

						break;
					}
					case Abilities.Guard:
					{
						GamePlayer playerOwner = GetPlayerOwner();

						if (playerOwner != null)
						{
							GuardAbilityHandler.CheckExistingEffectsOnTarget(Body, playerOwner, false, out bool foundOurEffect, out GuardECSGameEffect existingEffectFromAnotherSource);

							if (foundOurEffect)
								break;

							if (existingEffectFromAnotherSource != null)
								EffectService.RequestImmediateCancelEffect(existingEffectFromAnotherSource);

							new GuardECSGameEffect(new ECSGameEffectInitParams(Body, 0, 1, null), Body, playerOwner);
						}

						break;
					}
					case Abilities.Protect:
					{
						GamePlayer playerOwner = GetPlayerOwner();

						if (playerOwner != null)
						{
							ProtectAbilityHandler.CheckExistingEffectsOnTarget(Body, playerOwner, false, out bool foundOurEffect, out ProtectECSGameEffect existingEffectFromAnotherSource);

							if (foundOurEffect)
								break;

							if (existingEffectFromAnotherSource != null)
								EffectService.RequestImmediateCancelEffect(existingEffectFromAnotherSource);

							new ProtectECSGameEffect(new ECSGameEffectInitParams(Body, 0, 1, null), Body, playerOwner);
						}

						break;
					}
					case Abilities.ChargeAbility:
					{
						if (Body.TargetObject is GameLiving target &&
							GameServer.ServerRules.IsAllowedToAttack(Body, target, true) &&
							!Body.IsWithinRadius(target, 500))
						{
							ChargeAbility charge = Body.GetAbility<ChargeAbility>();

							if (charge != null && Body.GetSkillDisabledDuration(charge) <= 0)
								charge.Execute(Body);
						}

						break;
					}
				}
			}
		}

        protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
        {
            GameLiving target = null;

            switch (spell.SpellType)
            {
                #region Buffs

                case eSpellType.AcuityBuff:
                case eSpellType.AFHitsBuff:
                case eSpellType.AllMagicResistBuff:
                case eSpellType.AllSecondaryMagicResistsBuff:
                case eSpellType.ArmorAbsorptionBuff:
                case eSpellType.BaseArmorFactorBuff:
                case eSpellType.SpecArmorFactorBuff:
                case eSpellType.PaladinArmorFactorBuff:
                case eSpellType.BodyResistBuff:
                case eSpellType.BodySpiritEnergyBuff:
                case eSpellType.Buff:
                case eSpellType.CelerityBuff:
                case eSpellType.ColdResistBuff:
                case eSpellType.CombatSpeedBuff:
                case eSpellType.ConstitutionBuff:
                case eSpellType.CourageBuff:
                case eSpellType.CrushSlashTrustBuff:
                case eSpellType.DexterityBuff:
                case eSpellType.DexterityQuicknessBuff:
                case eSpellType.EffectivenessBuff:
                case eSpellType.EnduranceRegenBuff:
                case eSpellType.EnergyResistBuff:
                case eSpellType.FatigueConsumptionBuff:
                case eSpellType.FlexibleSkillBuff:
                case eSpellType.HasteBuff:
                case eSpellType.HealthRegenBuff:
                case eSpellType.HeatColdMatterBuff:
                case eSpellType.HeatResistBuff:
                case eSpellType.HeroismBuff:
                case eSpellType.KeepDamageBuff:
                case eSpellType.MagicResistBuff:
                case eSpellType.MatterResistBuff:
                case eSpellType.MeleeDamageBuff:
                case eSpellType.MesmerizeDurationBuff:
                case eSpellType.MLABSBuff:
                case eSpellType.ParryBuff:
                case eSpellType.PowerHealthEnduranceRegenBuff:
                case eSpellType.PowerRegenBuff:
                case eSpellType.SavageCombatSpeedBuff:
                case eSpellType.SavageCrushResistanceBuff:
                case eSpellType.SavageDPSBuff:
                case eSpellType.SavageParryBuff:
                case eSpellType.SavageSlashResistanceBuff:
                case eSpellType.SavageThrustResistanceBuff:
                case eSpellType.SpiritResistBuff:
                case eSpellType.StrengthBuff:
                case eSpellType.StrengthConstitutionBuff:
                case eSpellType.SuperiorCourageBuff:
                case eSpellType.ToHitBuff:
                case eSpellType.WeaponSkillBuff:
                case eSpellType.DamageAdd:
                case eSpellType.OffensiveProc:
                case eSpellType.DefensiveProc:
                case eSpellType.DamageShield:
                case eSpellType.Bladeturn:
                {
                    // Buff self.
                    if (!LivingHasEffect(Body, spell))
                    {
                        target = Body;
                        break;
                    }

                    if (spell.Target is eSpellTarget.REALM or eSpellTarget.GROUP)
                    {
                        GameLiving owner = (this as IControlledBrain).Owner;

                        // Buff owner.
                        if (!LivingHasEffect(owner, spell) && Body.IsWithinRadius(owner, spell.Range))
                        {
                            target = owner;
                            break;
                        }

                        if (owner is GameNPC npc)
                        {
                            //Buff other minions
                            foreach (IControlledBrain icb in npc.ControlledNpcList)
                            {
                                if (icb != null && icb.Body != null && !LivingHasEffect(icb.Body, spell) 
                                    && Body.IsWithinRadius(icb.Body, spell.Range))
                                {
                                    target = icb.Body;
                                    break;
                                }
                            }
                        }

                        GamePlayer player = GetPlayerOwner();

                        // Buff group members.
                        if (player != null)
                        {
                            if (!LivingHasEffect(player, spell))
                            {
                                target = player;
                                break;
                            }

                            if (player.Group != null)
                            {
                                foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
                                {
                                    if (!LivingHasEffect(member, spell) && Body.IsWithinRadius(member, spell.Range))
                                    {
                                        target = member;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    break;
                }

                #endregion Buffs

                #region Disease Cure/Poison Cure/Summon

                case eSpellType.CureDisease:
                {
                    GameLiving owner = (this as IControlledBrain).Owner;

                    // Cure owner.
                    if (owner.IsDiseased)
                    {
                        target = owner;
                        break;
                    }

                    // Cure self.
                    if (Body.IsDiseased)
                    {
                        target = Body;
                        break;
                    }

                    GamePlayer player = GetPlayerOwner();

                    // Cure group members.
                    if (player?.Group != null)
                    {
                        foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
                        {
                            if (member.IsDiseased && Body.IsWithinRadius(member, spell.Range))
                            {
                                target = member;
                                break;
                            }
                        }
                    }

                    break;
                }
                case eSpellType.CurePoison:
                {
                    GameLiving owner = (this as IControlledBrain).Owner;

                    // Cure owner.
                    if (owner.IsPoisoned)
                    {
                        target = owner;
                        break;
                    }

                    // Cure self.
                    if (Body.IsPoisoned)
                    {
                        target = Body;
                        break;
                    }

                    GamePlayer player = GetPlayerOwner();

                    // Cure group members.
                    if (player?.Group != null)
                    {
                        foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
                        {
                            if (member.IsPoisoned && Body.IsWithinRadius(member, spell.Range))
                            {
                                target = member;
                                break;
                            }
                        }
                    }
                     
                    break;
                }
                case eSpellType.Summon:
                {
                    target = Body;
                    break;
                }

                #endregion

                #region Heals

                case eSpellType.CombatHeal:
                case eSpellType.Heal:
                case eSpellType.HealOverTime:
                case eSpellType.MercHeal:
                case eSpellType.OmniHeal:
                case eSpellType.PBAoEHeal:
                case eSpellType.SpreadHeal:
                {
                    int bodyPercent = Body.HealthPercent;
                    int healThreshold = Properties.PET_HEAL_THRESHOLD;

                    if (spell.Target == eSpellTarget.SELF)
                    {
                        if (bodyPercent < healThreshold && !LivingHasEffect(Body, spell))
                            target = Body;

                        break;
                    }

                    // Heal seriously injured targets first.
                    int emergencyThreshold = healThreshold / 2;
                    int ownerPercent = Owner.HealthPercent;

                    // Heal owner.
                    if (ownerPercent < emergencyThreshold && !LivingHasEffect(Owner, spell) && Body.IsWithinRadius(Owner, spell.Range))
                    {
                        target = Owner;
                        break;
                    }

                    // Heal self.
                    if (bodyPercent < emergencyThreshold && !LivingHasEffect(Body, spell))
                    {
                        target = Body;
                        break;
                    }

                    ICollection<GamePlayer> playerGroup = null;
                    GamePlayer playerOwner = GetPlayerOwner();

                    // Heal group members.
                    if (playerOwner?.Group != null && (spell.Target is eSpellTarget.REALM or eSpellTarget.GROUP))
                    {
                        playerGroup = playerOwner.Group.GetPlayersInTheGroup();

                        foreach (GamePlayer member in playerGroup)
                        {
                            if (member.HealthPercent < emergencyThreshold && !LivingHasEffect(member, spell) && Body.IsWithinRadius(member, spell.Range))
                            {
                                target = member;
                                break;
                            }
                        }
                    }

                    // Now check for targets which aren't seriously injured.

                    if (spell.Target == eSpellTarget.SELF)
                    {
                        // If we have a self heal and health is less than 75% then heal, otherwise return false to try another spell or do nothing.
                        if (bodyPercent < healThreshold && !LivingHasEffect(Body, spell))
                            target = Body;

                        break;
                    }

                    // Heal owner
                    if (ownerPercent < healThreshold && !LivingHasEffect(Owner, spell) && Body.IsWithinRadius(Owner, spell.Range))
                    {
                        target = Owner;
                        break;
                    }

                    // Heal self.
                    if (bodyPercent < healThreshold && !LivingHasEffect(Body, spell))
                    {
                        target = Body;
                        break;
                    }

                    // Heal group members.
                    if (playerGroup != null)
                    {
                        foreach (GamePlayer member in playerGroup)
                        {
                            if (member.HealthPercent < healThreshold && !LivingHasEffect(member, spell) && Body.IsWithinRadius(member, spell.Range))
                            {
                                target = member;
                                break;
                            }
                        }
                    }

                    break;
                }

                #endregion
            }

            return target;
        }

		public override bool CanAggroTarget(GameLiving target)
		{
			GameLiving ownerToCheck = GetPlayerOwner();
			ownerToCheck ??= Owner;
			return AggroLevel > 0 && !ownerToCheck.IsObjectGreyCon(target) && GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
		}

		protected override bool ShouldBeRemovedFromAggroList(GameLiving living)
		{
			if (living.IsMezzed ||
				!living.IsAlive ||
				living.ObjectState is not GameObject.eObjectState.Active ||
				living.CurrentRegion != Body.CurrentRegion ||
				!Body.IsWithinRadius(living, MAX_AGGRO_LIST_DISTANCE) ||
				!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
			{
				return true;
			}

			ECSGameSpellEffect root = EffectListService.GetSpellEffectOnTarget(living, eEffect.MovementSpeedDebuff);
			return root != null && root.SpellHandler.Spell.Value == 99;
		}

		/// <summary>
		/// Perform some checks on 'm_orderAttackTarget'. Returns it if it's still a valid target, sets it to null otherwise.
		/// </summary>
		protected virtual GameLiving CheckAttackOrderTarget()
		{
			if (AggressionState != eAggressionState.Passive && m_orderAttackTarget != null)
			{
				if (m_orderAttackTarget.IsAlive &&
					m_orderAttackTarget.ObjectState == GameObject.eObjectState.Active &&
					GameServer.ServerRules.IsAllowedToAttack(Body, m_orderAttackTarget, true))
					return m_orderAttackTarget;

				m_orderAttackTarget = null;
			}

			return null;
		}

		protected override GameLiving CalculateNextAttackTarget()
		{
			return CheckAttackOrderTarget() ?? base.CalculateNextAttackTarget();
		}

		/// <summary>
		/// Selects and attacks the next target or does nothing
		/// </summary>
		public override void AttackMostWanted()
		{
			if (!IsActive || m_aggressionState == eAggressionState.Passive)
				return;

			GameNPC owner_npc = GetNPCOwner();

			if (owner_npc != null && owner_npc.Brain is StandardMobBrain)
			{
				if ((owner_npc.IsCasting || owner_npc.IsAttacking) &&
					owner_npc.TargetObject != null &&
					owner_npc.TargetObject is GameLiving &&
					GameServer.ServerRules.IsAllowedToAttack(owner_npc, owner_npc.TargetObject as GameLiving, false))
				{
					if (!CheckSpells(eCheckSpellType.Offensive))
						Body.StartAttack(owner_npc.TargetObject);

					return;
				}
			}

			GameLiving target = CalculateNextAttackTarget();
			
			if (target != null)
			{
				if (!Body.IsAttacking || target != Body.TargetObject)
				{
					Body.TargetObject = target;

					List<GameSpellEffect> effects = new List<GameSpellEffect>();

					lock (Body.EffectList.Lock)
					{
						foreach (IGameEffect effect in Body.EffectList)
						{
							if (effect is GameSpellEffect gameSpellEffect && gameSpellEffect.SpellHandler is SpeedEnhancementSpellHandler)
								effects.Add(gameSpellEffect);
						}
					}

					lock (Owner.EffectList.Lock)
					{
						foreach (IGameEffect effect in Owner.EffectList)
						{
							if (effect is GameSpellEffect gameSpellEffect && gameSpellEffect.SpellHandler is SpeedEnhancementSpellHandler)
								effects.Add(gameSpellEffect);
						}
					}

					foreach (GameSpellEffect effect in effects)
						effect.Cancel(false);
				}

				if (!CheckSpells(eCheckSpellType.Offensive))
					Body.StartAttack(target);
			}
			else
			{
				if (Body.IsAttacking)
					Disengage();

				if (WalkState == eWalkState.Follow)
					FollowOwner();
				else if (m_tempX > 0 && m_tempY > 0 && m_tempZ > 0)
				{
					Body.StopFollowing();
					Body.WalkTo(new Point3D(m_tempX, m_tempY, m_tempZ), Body.MaxSpeed);
					// TODO: Should the cached position be cleared?
				}
			}
		}

		public virtual void OnOwnerAttacked(AttackData ad)
		{
			if (FSM.GetCurrentState() == FSM.GetState(eFSMStateType.PASSIVE))
				return;

			// Theurgist pets don't help their owner.
			if (Owner is GamePlayer playerOwner && (eCharacterClass) playerOwner.CharacterClass.ID is eCharacterClass.Theurgist)
				return;

			switch (ad.AttackResult)
			{
				case eAttackResult.Blocked:
				case eAttackResult.Evaded:
				case eAttackResult.Fumbled:
				case eAttackResult.HitStyle:
				case eAttackResult.HitUnstyled:
				case eAttackResult.Missed:
				case eAttackResult.Parried:
				{
					ConvertAttackToAggroAmount(ad);
				}

				break;
			}
		}

		public virtual void OnRelease()
		{
			StripCastedBuffs();

			foreach (ECSGameSpellEffect effect in Body.effectListComponent.GetSpellEffects())
			{
				if (effect.EffectType is eEffect.Pet or eEffect.Charm)
					EffectService.RequestImmediateCancelEffect(effect);
			}
		}

		public void AddBuffedTarget(GameLiving living)
		{
			if (living == Body)
				return;

			lock (_buffedTargetsLock)
			{
				_buffedTargets.Add(living);
			}
		}

		public void StripCastedBuffs()
		{
			lock (_buffedTargetsLock)
			{
				foreach (GameLiving living in _buffedTargets)
				{
					foreach (ECSGameEffect effect in living.effectListComponent.GetAllEffects().Where(x => x.SpellHandler != null && x.SpellHandler.Caster == Body))
						EffectService.RequestCancelEffect(effect);
				}

				_buffedTargets.Clear();
			}
		}

		public virtual int ModifyDamageWithTaunt(int damage) { return damage; }

		protected override void BringFriends(GameLiving trigger) { }

		public override bool CheckFormation(ref int x, ref int y, ref int z) { return false; }

		#endregion
	}
}
