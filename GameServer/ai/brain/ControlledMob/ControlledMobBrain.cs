using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;

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
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		public const int MAX_PET_AGGRO_DISTANCE = 512; // Tolakram - Live test with caby pet - I was extremely close before auto aggro
		public const short MIN_OWNER_FOLLOW_DIST = 80;
		public const short MAX_OWNER_FOLLOW_DIST = 10000;

		protected Vector3? _tempPosition;

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

		public override int ThinkInterval => Properties.PET_THINK_INTERVAL;
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
                    Body?.effectListComponent.RequestPlayerUpdate(EffectHelper.PlayerUpdate.Icons);

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
                    Body?.effectListComponent.RequestPlayerUpdate(EffectHelper.PlayerUpdate.Icons);

                m_aggressionState = value;

                if (m_aggressionState is eAggressionState.Passive)
                {
                    Disengage();
                    ResumeWalkState();
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
			_tempPosition = new(Body.X, Body.Y, Body.Z);
			WalkState = eWalkState.Stay;
			Body.StopMoving();
		}

		/// <summary>
		/// Go to owner on command
		/// </summary>
		public virtual void ComeHere()
		{
			_tempPosition = new(Owner.X, Owner.Y, Owner.Z);
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
			_tempPosition = new(target.X, target.Y, target.Z);
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
								existingEffectFromAnotherSource.End();

							ECSGameEffectFactory.Create(new(Body, 0, 1, null), Body, playerOwner, static (in i, body, owner) => new InterceptECSGameEffect(i, body, owner));
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
								existingEffectFromAnotherSource.End();

							ECSGameEffectFactory.Create(new(Body, 0, 1), Body, playerOwner, static (in i, body, owner) => new GuardECSGameEffect(i, body, owner));
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
								existingEffectFromAnotherSource.End();

							ECSGameEffectFactory.Create(new(Body, 0, 1), Body, playerOwner, static (in i, body, owner) => new ProtectECSGameEffect(i, body, owner));
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

		protected virtual bool CanCastDefensiveSpellsOnGroupMembers => true;
		protected override int HealThreshold => Properties.PET_HEAL_THRESHOLD;
		protected override bool UseEmergencyHeal => true;

		protected override List<GameLiving> GetPrioritizedTargetsForDefensiveSpell(Spell spell)
		{
			// Prioritization order: Player owner, npc owner, self, own minions, npc owner's minion, group members (if allowed).

			GameLiving owner = Owner;
			List<GameLiving> candidates = GameLoop.GetListForTick<GameLiving>();

			GamePlayer playerOwner = null;
			GameNPC npcOwner = null;

			if (spell.Target is not eSpellTarget.SELF)
			{
				playerOwner = GetPlayerOwner();

				if (playerOwner != null)
					candidates.Add(playerOwner);

				npcOwner = owner as GameNPC;

				if (npcOwner != null)
					candidates.Add(owner);
			}

			candidates.Add(Body);

			if (spell.Target is not eSpellTarget.SELF)
			{
				IControlledBrain[] controlledNpcList = Body.ControlledNpcList;

				if (controlledNpcList != null)
				{
					foreach (IControlledBrain brain in controlledNpcList)
					{
						if (brain?.Body != null)
							candidates.Add(brain.Body);
					}
				}

				if (npcOwner != null)
				{
					controlledNpcList = npcOwner.ControlledNpcList;

					if (controlledNpcList != null)
					{
						foreach (IControlledBrain brain in controlledNpcList)
						{
							if (brain?.Body != null)
								candidates.Add(brain.Body);
						}
					}
				}

				if (CanCastDefensiveSpellsOnGroupMembers)
				{
					List<GamePlayer> groupMembers = playerOwner?.Group?.GetPlayersInTheGroup();

					if (groupMembers != null)
					{
						foreach (GamePlayer member in groupMembers)
						{
							// Avoid duplicate.
							if (member != playerOwner)
								candidates.Add(member);
						}
					}
				}
			}

			return candidates;
		}

		public override bool CanSpellStillBeCastOnTarget(Spell spell, GameLiving target)
		{
			if (target == null)
				return false;

			// Special case for underhill ally. It cannot heal itself.
			// This should be moved to an underhill ally specific brain.
			if (spell.ID == 60015 && target == Body)
				return false;

			return base.CanSpellStillBeCastOnTarget(spell, target);
		}

		public override bool CanAggroTarget(GameLiving target)
		{
			GameLiving ownerToCheck = GetPlayerOwner();
			ownerToCheck ??= Owner;
			return AggroLevel > 0 && !ownerToCheck.IsObjectGreyCon(target) && GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
		}

		/// <summary>
		/// Perform some checks on 'm_orderAttackTarget'. Returns it if it's still a valid target, sets it to null otherwise.
		/// </summary>
		protected virtual GameLiving CheckAttackOrderTarget()
		{
			if (m_orderAttackTarget == null)
				return null;

			if (!m_orderAttackTarget.IsAlive ||
				m_orderAttackTarget.ObjectState is not GameObject.eObjectState.Active ||
				!GameServer.ServerRules.IsAllowedToAttack(Body, m_orderAttackTarget, true))
			{
				m_orderAttackTarget = null;
				return null;
			}

			return m_orderAttackTarget;
		}

		protected override AggroTable BuildAggroTable()
		{
			return new(new ControlledNpcThreatStrategy(this));
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
			if (!IsActive)
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

			if (target == null)
			{
				Body.StopAttack();
				return;
			}

			Body.TargetObject = target;

			if (CheckSpells(eCheckSpellType.Offensive))
				Body.StopAttack();
			else
				Body.StartAttack(target);
		}

		public override void Disengage()
		{
			m_orderAttackTarget = null;
			base.Disengage();
		}

		public void ResumeWalkState()
		{
			if (WalkState is eWalkState.Follow)
				FollowOwner();
			else if (_tempPosition.HasValue)
			{
				Body.StopMoving();
				Body.PathTo(_tempPosition.Value, Body.MaxSpeed);
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
					effect.End();
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
					foreach (ECSGameEffect effect in living.effectListComponent.GetEffects().Where(x => x.SpellHandler != null && x.SpellHandler.Caster == Body))
						effect.End();
				}

				_buffedTargets.Clear();
			}
		}

		public virtual int ModifyDamageWithTaunt(int damage) { return damage; }

		protected override void BringFriends(GameLiving trigger) { }

		public override bool CheckFormation(ref int x, ref int y, ref int z) { return false; }

		#endregion

		protected class ControlledNpcThreatStrategy : ThreatStrategy
		{
			public ControlledNpcThreatStrategy(StandardMobBrain owner) : base(owner) { }

			public override bool ShouldBeRemoved(GameLiving target)
			{
				if (base.ShouldBeRemoved(target))
					return true;

				// Pets forget about mezzed and rooted players.
				if (target.IsMezzed)
					return true;

				ECSGameEffect root = EffectListService.GetEffectOnTarget(target, eEffect.MovementSpeedDebuff);
				return root != null && root.SpellHandler.Spell.Value == 99;
			}
		}
	}
}
