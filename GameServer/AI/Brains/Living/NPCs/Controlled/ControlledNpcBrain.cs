using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.States;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.GS.RealmAbilities;
using Core.GS.SkillHandler;
using Core.GS.Spells;
using log4net;

namespace Core.GS.AI.Brains
{
	/// <summary>
	/// A brain that can be controlled
	/// </summary>
	public class ControlledNpcBrain : StandardMobBrain, IControlledBrain
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
		public const short MIN_ENEMY_FOLLOW_DIST = 90;
		public const short MAX_ENEMY_FOLLOW_DIST = 5000;

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
		protected EWalkState m_walkState;

		/// <summary>
		/// Holds the aggression level of the brain
		/// </summary>
		protected EAggressionState m_aggressionState;

		private HashSet<GameLiving> m_buffedTargets = new();
		private object m_buffedTargetsLock = new();

		/// <summary>
		/// Constructs new controlled npc brain
		/// </summary>
		/// <param name="owner"></param>
		public ControlledNpcBrain(GameLiving owner) : base()
		{
			m_owner = owner ?? throw new ArgumentNullException("owner");
			m_aggressionState = EAggressionState.Defensive;
			m_walkState = EWalkState.Follow;

			if (owner is GameNpc npcOwner && npcOwner.Brain is StandardMobBrain npcOwnerBrain)
				AggroLevel = npcOwnerBrain.AggroLevel;
			else
				AggroLevel = 99;
			AggroRange = MAX_PET_AGGRO_DISTANCE;

			FiniteStateMachine.ClearStates();

			FiniteStateMachine.Add(new ControlledNpcStateWakingUp(this));
			FiniteStateMachine.Add(new ControlledNpcStatePassive(this));
			FiniteStateMachine.Add(new ControlledNpcStateDefensive(this));
			FiniteStateMachine.Add(new ControlledNpcStateAggro(this));
			FiniteStateMachine.Add(new StandardNpcStateDead(this));

			FiniteStateMachine.SetCurrentState(EFSMStateType.WAKING_UP);
		}

		protected bool m_isMainPet = true;
		public bool checkAbility;
		public bool sortedSpells;

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
            while (owner is GameNpc && owner != null)
            {
                i++;
                if (i > 50)
                    throw new Exception("GetPlayerOwner() from " + Owner.Name + "caused a cyclical loop.");
                //If this is a pet, get its owner
                if (((GameNpc)owner).Brain is IControlledBrain)
                    owner = ((IControlledBrain)((GameNpc)owner).Brain).Owner;
                //This isn't a pet, that means it's at the top of the tree.  This case will only happen if
                //owner is not a GamePlayer
                else
                    break;
            }
            //Return if we found the gameplayer
            if (owner is GamePlayer)
                return (GamePlayer)owner;
            //If the root owner was not a player or npc then make sure we know that something went wrong!
            if (!(owner is GameNpc))
                throw new Exception("Unrecognized owner: " + owner.GetType().FullName);
            //No GamePlayer at the top of the tree
            return null;
        }

        public virtual GameNpc GetNPCOwner()
        {
            if (!(Owner is GameNpc))
                return null;

            GameNpc owner = Owner as GameNpc;

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
                        owner = (owner.Brain as IControlledBrain).Owner as GameNpc;
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

            GameNpc npc = GetNPCOwner();
            if (npc != null)
                return npc;

            return null;
        }

		/// <summary>
		/// Gets or sets the walk state of the brain
		/// </summary>
		public virtual EWalkState WalkState
		{
			get { return m_walkState; }
			set
			{
				m_walkState = value;
				UpdatePetWindow();
			}
		}

		/// <summary>
		/// Gets or sets the aggression state of the brain
		/// </summary>
		public virtual EAggressionState AggressionState
        {
            get => m_aggressionState;
            set
            {
                m_aggressionState = value;

                if (m_aggressionState == EAggressionState.Passive)
                {
                    Disengage();

                    if (WalkState == EWalkState.Follow)
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
			if (AggressionState == EAggressionState.Passive)
			{
				AggressionState = EAggressionState.Defensive;
				UpdatePetWindow();
			}

			if (m_orderAttackTarget == target)
				return;

			m_orderAttackTarget = target as GameLiving;
			FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO);

			if (target != Body.TargetObject && Body.IsCasting)
				Body.StopCurrentSpellcast();

			AttackMostWanted();
		}

		public virtual void Disengage()
		{
			// We switch to defensive mode if we're in aggressive and have a target, so that we don't immediately aggro back
			if (AggressionState == EAggressionState.Aggressive && Body.TargetObject != null)
			{
				AggressionState = EAggressionState.Defensive;
				UpdatePetWindow();
			}

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
			WalkState = EWalkState.Follow;
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
			WalkState = EWalkState.Stay;
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
			WalkState = EWalkState.ComeHere;
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
			WalkState = EWalkState.GoTarget;
			Body.StopFollowing();
			Body.PathTo(target, Body.MaxSpeed);
		}

		public virtual void SetAggressionState(EAggressionState state)
		{
			AggressionState = state;
			UpdatePetWindow();
		}

		/// <summary>
		/// Updates the pet window
		/// </summary>
		public virtual void UpdatePetWindow()
		{
			if (m_owner is GamePlayer)
				((GamePlayer)m_owner).Out.SendPetWindow(Body, EPetWindowAction.Update, m_aggressionState, m_walkState);
		}

		/// <summary>
		/// Start following the owner
		/// </summary>
		public virtual void FollowOwner()
		{
			if (Body.IsAttacking)
				Body.StopAttack();
				
			if (Owner is GamePlayer
			    && IsMainPet
			    && ((GamePlayer)Owner).PlayerClass.ID != (int)EPlayerClass.Animist
			    && ((GamePlayer)Owner).PlayerClass.ID != (int)EPlayerClass.Theurgist)
				Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
			else if (Owner is GameNpc)
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

			if (WalkState == EWalkState.Follow)
				FollowOwner();

			return true;
		}

		/// <summary>
		/// Stops the brain thinking
		/// </summary>
		/// <returns>true if stopped</returns>
		public override bool Stop()
		{
			if (!base.Stop()) return false;
			//GameEventMgr.RemoveHandler(Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnOwnerAttacked));

			GameEventMgr.Notify(GameLivingEvent.PetReleased, Body);
			return true;
		}

		/// <summary>
		/// Do the mob AI
		/// </summary>
		public override void Think()
		{
			base.Think();
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
						// The pet should intercept even if a player is still intercepting for the owner.
						GamePlayer playerOwner = GetPlayerOwner();

						if (playerOwner != null)
							new InterceptEcsAbilityEffect(new EcsGameEffectInitParams(Body, 0, 1), Body, playerOwner);

						break;
					}
					case Abilities.Guard:
					{
						GamePlayer playerOwner = GetPlayerOwner();

						if (playerOwner != null)
						{
							GuardAbilityHandler.CheckExistingEffectsOnTarget(Body, playerOwner, false, out bool foundOurEffect, out GuardEcsAbilityEffect existingEffectFromAnotherSource);

							if (foundOurEffect)
								break;

							if (existingEffectFromAnotherSource == null)
								GuardAbilityHandler.CancelOurEffectThenAddOnTarget(Body, playerOwner);
						}

						break;
					}
					case Abilities.Protect:
					{
						GamePlayer playerOwner = GetPlayerOwner();

						if (playerOwner != null)
							new ProtectEcsAbilityEffect(new EcsGameEffectInitParams(playerOwner, 0, 1), null, playerOwner);

						break;
					}
					case Abilities.ChargeAbility:
					{
						if (Body.TargetObject is GameLiving target &&
							GameServer.ServerRules.IsAllowedToAttack(Body, target, true) &&
							!Body.IsWithinRadius(target, 500))
						{
							NfRaChargeAbility charge = Body.GetAbility<NfRaChargeAbility>();

							if (charge != null && Body.GetSkillDisabledDuration(charge) <= 0)
								charge.Execute(Body);
						}

						break;
					}
				}
			}
		}

		/// <summary>
		/// Checks if any spells need casting
		/// </summary>
		/// <param name="type">Which type should we go through and check for?</param>
		/// <returns>True if we are begin to cast or are already casting</returns>
		public override bool CheckSpells(ECheckSpellType type)
		{
			if (Body == null || Body.Spells == null || Body.Spells.Count < 1)
				return false;
			
			bool casted = false;
			if (type == ECheckSpellType.Defensive)
			{
				// Check instant spells, but only cast one of each type to prevent spamming
				if (Body.CanCastInstantHealSpells)
				{
					foreach (Spell spell in Body.InstantHealSpells)
					{
						if (CheckDefensiveSpells(spell))
							break;
					}
				}

				if (Body.CanCastInstantMiscSpells)
				{
					foreach (Spell spell in Body.InstantMiscSpells)
					{
						if (CheckDefensiveSpells(spell))
							break;
					}
				}

				// Check spell lists, prioritizing healing
				if (Body.CanCastHealSpells)
				{
					foreach (Spell spell in Body.HealSpells)
					{
						if (CheckDefensiveSpells(spell))
						{
							casted = true;
							break;
						}
					}
				}

				if (!casted && Body.CanCastMiscSpells)
				{
					foreach (Spell spell in Body.MiscSpells)
					{
						if (CheckDefensiveSpells(spell))
						{
							casted = true;
							break;
						}
					}
				}
			}
			else if (Body.TargetObject is GameLiving living && living.IsAlive)
			{
				// Check instant spells, but only cast one to prevent spamming
				if (Body.CanCastInstantHarmfulSpells)
				{
					foreach (Spell spell in Body.InstantHarmfulSpells)
					{
						if (CheckOffensiveSpells(spell))
							break;
					}
				}

				if (Body.CanCastHarmfulSpells)
				{
					foreach (Spell spell in Body.HarmfulSpells)
					{
						if (CheckOffensiveSpells(spell))
						{
							casted = true;
							break;
						}
					}
				}
			}

			return casted || Body.IsCasting;
		}

        /// <summary>
        /// Checks the Positive Spells.  Handles buffs, heals, etc.
        /// </summary>
        protected override bool CheckDefensiveSpells(Spell spell)
        {
            if (!CanCastDefensiveSpell(spell))
                return false;

            bool casted = false;
            Body.TargetObject = null;
            GamePlayer player;
            GameLiving owner;

            switch (spell.SpellType)
            {
                #region Buffs
                case ESpellType.AcuityBuff:
                case ESpellType.AFHitsBuff:
                case ESpellType.AllMagicResistBuff:
                case ESpellType.ArmorAbsorptionBuff:
                case ESpellType.ArmorFactorBuff:
                case ESpellType.BodyResistBuff:
                case ESpellType.BodySpiritEnergyBuff:
                case ESpellType.Buff:
                case ESpellType.CelerityBuff:
                case ESpellType.ColdResistBuff:
                case ESpellType.CombatSpeedBuff:
                case ESpellType.ConstitutionBuff:
                case ESpellType.CourageBuff:
                case ESpellType.CrushSlashTrustBuff:
                case ESpellType.DexterityBuff:
                case ESpellType.DexterityQuicknessBuff:
                case ESpellType.EffectivenessBuff:
                case ESpellType.EnduranceRegenBuff:
                case ESpellType.EnergyResistBuff:
                case ESpellType.FatigueConsumptionBuff:
                case ESpellType.FlexibleSkillBuff:
                case ESpellType.HasteBuff:
                case ESpellType.HealthRegenBuff:
                case ESpellType.HeatColdMatterBuff:
                case ESpellType.HeatResistBuff:
                case ESpellType.HeroismBuff:
                case ESpellType.KeepDamageBuff:
                case ESpellType.MagicResistBuff:
                case ESpellType.MatterResistBuff:
                case ESpellType.MeleeDamageBuff:
                case ESpellType.MesmerizeDurationBuff:
                case ESpellType.MLABSBuff:
                case ESpellType.PaladinArmorFactorBuff:
                case ESpellType.ParryBuff:
                case ESpellType.PowerHealthEnduranceRegenBuff:
                case ESpellType.PowerRegenBuff:
                case ESpellType.SavageCombatSpeedBuff:
                case ESpellType.SavageCrushResistanceBuff:
                case ESpellType.SavageDPSBuff:
                case ESpellType.SavageParryBuff:
                case ESpellType.SavageSlashResistanceBuff:
                case ESpellType.SavageThrustResistanceBuff:
                case ESpellType.SpiritResistBuff:
                case ESpellType.StrengthBuff:
                case ESpellType.StrengthConstitutionBuff:
                case ESpellType.SuperiorCourageBuff:
                case ESpellType.ToHitBuff:
                case ESpellType.WeaponSkillBuff:
                case ESpellType.DamageAdd:
                case ESpellType.OffensiveProc:
                case ESpellType.DefensiveProc:
                case ESpellType.DamageShield:
                case ESpellType.Bladeturn:
                    {
						// Buff self
						if (!LivingHasEffect(Body, spell))
						{
                            Body.TargetObject = Body;
							break;
						}
						
						if (spell.Target is ESpellTarget.REALM or ESpellTarget.GROUP)
						{
							owner = (this as IControlledBrain).Owner;

							// Buff owner
							if (!LivingHasEffect(owner, spell) && Body.IsWithinRadius(owner, spell.Range))
							{
                                Body.TargetObject = owner;
								break;
							}

							if (owner is GameNpc npc)
							{
								//Buff other minions
								foreach (IControlledBrain icb in npc.ControlledNpcList)
								{
									if (icb != null && icb.Body != null && !LivingHasEffect(icb.Body, spell) 
										&& Body.IsWithinRadius(icb.Body, spell.Range))
									{
                                        Body.TargetObject = icb.Body;
										break;
									}
								}
							}

							player = GetPlayerOwner();

							// Buff player
							if (player != null)
							{
								if (!LivingHasEffect(player, spell))
								{
                                    Body.TargetObject = player;
									break;
								}

								if (player.Group != null)
								{
									foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
									{
										if (!LivingHasEffect(member, spell) && Body.IsWithinRadius(member, spell.Range))
										{
                                            Body.TargetObject = member;
											break;
										}
									}
								}
							}
						}
					}
					break;
                #endregion Buffs

                #region Disease Cure/Poison Cure/Summon
                case ESpellType.CureDisease:
					//Cure owner
					owner = (this as IControlledBrain).Owner;
					if (owner.IsDiseased)
					{
						Body.TargetObject = owner;
						break;
					}

					//Cure self
					if (Body.IsDiseased)
					{
						Body.TargetObject = Body;
						break;
					}

					// Cure group members

					player = GetPlayerOwner();

					if (player.Group != null)
					{
						foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
						{
							if (p.IsDiseased && Body.IsWithinRadius(p, spell.Range))
							{
								Body.TargetObject = p;
								break;
							}
						}
					}
					break;
                case ESpellType.CurePoison:
					//Cure owner
					owner = (this as IControlledBrain).Owner;
					if (LivingIsPoisoned(owner))
					{
						Body.TargetObject = owner;
						break;
					}

					//Cure self
					if (LivingIsPoisoned(Body))
					{
						Body.TargetObject = Body;
						break;
					}

					// Cure group members

					player = GetPlayerOwner();

					if (player.Group != null)
					{
						foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
						{
							if (LivingIsPoisoned(p) && Body.IsWithinRadius(p, spell.Range))
							{
								Body.TargetObject = p;
								break;
							}
						}
					}
					break;
                case ESpellType.Summon:
					Body.TargetObject = Body;
					break;
                #endregion

                #region Heals
                case ESpellType.CombatHeal:
                case ESpellType.Heal:
                case ESpellType.HealOverTime:
                case ESpellType.MercHeal:
                case ESpellType.OmniHeal:
                case ESpellType.PBAoEHeal:
                case ESpellType.SpreadHeal:
					int bodyPercent = Body.HealthPercent;
					//underhill ally heals at half the normal threshold 'will heal seriously injured groupmates'
					int healThreshold = this.Body.Name.Contains("underhill") ? GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD / 2 : GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD;

					if (Body.Name.Contains("empyrean"))
					{
						healThreshold = this.Body.Name.Contains("empyrean") ? GS.ServerProperties.Properties.CHARMED_NPC_HEAL_THRESHOLD : GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD;
					}

					if (spell.Target == ESpellTarget.SELF)
					{
						if (bodyPercent < healThreshold && !LivingHasEffect(Body, spell))
							Body.TargetObject = Body;

						break;
					}

					// Heal seriously injured targets first
					int emergencyThreshold = healThreshold / 2;

					//Heal owner
					owner = (this as IControlledBrain).Owner;
					int ownerPercent = owner.HealthPercent;
					if (ownerPercent < emergencyThreshold && !LivingHasEffect(owner, spell) && Body.IsWithinRadius(owner, spell.Range))
					{
						Body.TargetObject = owner;
						break;
					}

					//Heal self
					if (bodyPercent < emergencyThreshold
						&& !LivingHasEffect(Body, spell))
					{
						Body.TargetObject = Body;
						break;
					}

					// Heal group
					player = GetPlayerOwner();
					ICollection<GamePlayer> playerGroup = null;
					if (player.Group != null && (spell.Target is ESpellTarget.REALM or ESpellTarget.GROUP))
					{
						playerGroup = player.Group.GetPlayersInTheGroup();

						foreach (GamePlayer p in playerGroup)
						{
							if (p.HealthPercent < emergencyThreshold && !LivingHasEffect(p, spell)
								&& Body.IsWithinRadius(p, spell.Range))
							{
								Body.TargetObject = p;
								break;
							}
						}
					}

					// Now check for targets which aren't seriously injured

					if (spell.Target == ESpellTarget.SELF)
					{
						// if we have a self heal and health is less than 75% then heal, otherwise return false to try another spell or do nothing
						if (bodyPercent < healThreshold
							&& !LivingHasEffect(Body, spell))
						{
							Body.TargetObject = Body;
						}
						break;
					}

					//Heal owner
					owner = (this as IControlledBrain).Owner;
					if (ownerPercent < healThreshold
						&& !LivingHasEffect(owner, spell) && Body.IsWithinRadius(owner, spell.Range))
					{
						Body.TargetObject = owner;
						break;
					}

					//Heal self
					if (bodyPercent < healThreshold
						&& !LivingHasEffect(Body, spell))
					{
						Body.TargetObject = Body;
						break;
					}

					// Heal group
					if (playerGroup != null)
					{
						foreach (GamePlayer p in playerGroup)
						{
							if (p.HealthPercent < healThreshold
								&& !LivingHasEffect(p, spell) && Body.IsWithinRadius(p, spell.Range))
							{
								Body.TargetObject = p;
								break;
							}
						}
					}
					break;
				#endregion

				default:
					log.Warn($"CheckDefensiveSpells() encountered an unknown spell type [{spell.SpellType}], calling base method");
				return base.CheckDefensiveSpells(spell);
			}

			if (Body.TargetObject != null)
				casted = Body.CastSpell(spell, m_mobSpellLine, true);

			return casted;
		}

		// Temporary until StandardMobBrain is updated
		protected override bool CheckOffensiveSpells(Spell spell)
		{		
			if (spell == null || spell.IsHelpful || !(Body.TargetObject is GameLiving living) || !living.IsAlive)
				return false;

			// Make sure we're currently able to cast the spell
			if (spell.CastTime > 0 && Body.IsBeingInterrupted && !spell.Uninterruptible)
				return false;

			// Make sure the spell isn't disabled
			if (spell.HasRecastDelay && Body.GetSkillDisabledDuration(spell) > 0)
				return false;

			if (!Body.IsWithinRadius(Body.TargetObject, spell.Range))
				return false;

			return base.CheckOffensiveSpells(spell);
		}

		/// <summary>
		/// Lost follow target event
		/// </summary>
		/// <param name="target"></param>
		protected override void OnFollowLostTarget(GameObject target)
		{
			if (target == Owner)
			{
				GameEventMgr.Notify(GameLivingEvent.PetReleased, Body);
				return;
			}

			FollowOwner();
		}

		public override bool CanAggroTarget(GameLiving target)
		{
			// Only attack if target (or target's owner) is green+ to our owner
			if (target is GameNpc npc && npc.Brain is IControlledBrain controlledBrain && controlledBrain.Owner != null)
				target = controlledBrain.Owner;

			if (!GameServer.ServerRules.IsAllowedToAttack(Body, target, true) || Owner.IsObjectGreyCon(target))
				return false;

			return AggroLevel > 0;
		}

		protected override bool ShouldThisLivingBeFilteredOutFromAggroList(GameLiving living)
		{
			if (living.IsMezzed ||
				!living.IsAlive ||
				living.ObjectState != GameObject.eObjectState.Active ||
				living.CurrentRegion != Body.CurrentRegion ||
				!Body.IsWithinRadius(living, MAX_AGGRO_LIST_DISTANCE) ||
				!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
				return true;
			else
			{
				EcsGameSpellEffect root = EffectListService.GetSpellEffectOnTarget(living, EEffect.MovementSpeedDebuff);

				if (root != null && root.SpellHandler.Spell.Value == 99)
					return true;
			}
			
			return false;
		}

		/// <summary>
		/// Perform some checks on 'm_orderAttackTarget'. Returns it if it's still a valid target, sets it to null otherwise.
		/// </summary>
		protected virtual GameLiving CheckAttackOrderTarget()
		{
			if (AggressionState != EAggressionState.Passive && m_orderAttackTarget != null)
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
			if (!IsActive || m_aggressionState == EAggressionState.Passive)
				return;

			GameNpc owner_npc = GetNPCOwner();

			if (owner_npc != null && owner_npc.Brain is StandardMobBrain)
			{
				if ((owner_npc.IsCasting || owner_npc.IsAttacking) &&
					owner_npc.TargetObject != null &&
					owner_npc.TargetObject is GameLiving &&
					GameServer.ServerRules.IsAllowedToAttack(owner_npc, owner_npc.TargetObject as GameLiving, false))
				{

					if (!CheckSpells(ECheckSpellType.Offensive))
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

					lock (Body.EffectList)
					{
						foreach (IGameEffect effect in Body.EffectList)
						{
							if (effect is GameSpellEffect gameSpellEffect && gameSpellEffect.SpellHandler is SpeedEnhancementSpell)
								effects.Add(gameSpellEffect);
						}
					}

					lock (Owner.EffectList)
					{
						foreach (IGameEffect effect in Owner.EffectList)
						{
							if (effect is GameSpellEffect gameSpellEffect && gameSpellEffect.SpellHandler is SpeedEnhancementSpell)
								effects.Add(gameSpellEffect);
						}
					}

					foreach (GameSpellEffect effect in effects)
						effect.Cancel(false);
				}

				if (!CheckSpells(ECheckSpellType.Offensive))
				{
					Body.StartAttack(target);

					if (Body.FollowTarget != target)
						Body.Follow(target, MIN_ENEMY_FOLLOW_DIST, MAX_ENEMY_FOLLOW_DIST);
				}
			}
			else
			{
				Body.TargetObject = null;

				if (Body.IsAttacking)
					Body.StopAttack();

				if (WalkState == EWalkState.Follow)
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
			if(FiniteStateMachine.GetState(EFSMStateType.PASSIVE) == FiniteStateMachine.GetCurrentState()) { return; }

			// Theurgist pets don't help their owner.
			if (Owner is GamePlayer && ((GamePlayer)Owner).PlayerClass.ID == (int)EPlayerClass.Theurgist)
				return;

			if (ad.Target is GamePlayer && ((ad.Target as GamePlayer).ControlledBrain != this || (ad.Target as GamePlayer).ControlledBrain.Body == Owner))
				return;

			switch (ad.AttackResult)
			{
				case EAttackResult.Blocked:
				case EAttackResult.Evaded:
				case EAttackResult.Fumbled:
				case EAttackResult.HitStyle:
				case EAttackResult.HitUnstyled:
				case EAttackResult.Missed:
				case EAttackResult.Parried:
					AddToAggroList(ad.Attacker, ad.Attacker.EffectiveLevel + ad.Damage + ad.CriticalDamage);
					break;
			}

			if (FiniteStateMachine.GetState(EFSMStateType.AGGRO) != FiniteStateMachine.GetCurrentState()) { FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO); }
			AttackMostWanted();
		}

		public void AddBuffedTarget(GameLiving living)
		{
			if (living == Body)
				return;

			lock (m_buffedTargetsLock)
			{
				m_buffedTargets.Add(living);
			}
		}

		public void StripCastedBuffs()
		{
			lock (m_buffedTargetsLock)
			{
				foreach (GameLiving living in m_buffedTargets)
				{
					foreach (EcsGameEffect effect in living.effectListComponent.GetAllEffects().Where(x => x.SpellHandler.Caster == Body))
						EffectService.RequestCancelEffect(effect);
				}

				m_buffedTargets.Clear();
			}
		}

		public virtual int ModifyDamageWithTaunt(int damage) { return damage; }

		protected override void BringFriends(GameLiving trigger) { }

		public override bool CheckFormation(ref int x, ref int y, ref int z) { return false; }

		#endregion
	}
}
