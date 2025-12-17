using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Standard brain for standard mobs
    /// </summary>
    public class StandardMobBrain : APlayerVicinityBrain, IOldAggressiveBrain
    {
        public const int MAX_AGGRO_DISTANCE = 3600;
        public const int MAX_AGGRO_LIST_DISTANCE = 6000;

        // Effective aggro reduction is calculated using an exponential decay function, starting from the distance threshold. A reduction of 2/3rd is ensured at 1500.
        private const int EFFECTIVE_AGGRO_DISTANCE_THRESHOLD = 250; // Should be higher than players' melee range.
        private static readonly double EFFECTIVE_AGGRO_EXPONENT = Math.Log(1 / 3.0) / (1500 - EFFECTIVE_AGGRO_DISTANCE_THRESHOLD);

        // Used for AmbientBehaviour "Seeing" - maintains a list of GamePlayer in range
        public List<GamePlayer> PlayersSeen = new();

        /// <summary>
        /// Constructs a new StandardMobBrain
        /// </summary>
        public StandardMobBrain() : base()
        {
            FSM = new FSM();
            FSM.Add(new StandardMobState_WAKING_UP(this));
            FSM.Add(new StandardMobState_IDLE(this));
            FSM.Add(new StandardMobState_AGGRO(this));
            FSM.Add(new StandardMobState_RETURN_TO_SPAWN(this));
            FSM.Add(new StandardMobState_PATROLLING(this));
            FSM.Add(new StandardMobState_ROAMING(this));
        }

        /// <summary>
        /// Returns the string representation of the StandardMobBrain
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + ", AggroLevel=" + AggroLevel.ToString() + ", AggroRange=" + AggroRange.ToString();
        }

        public override bool Stop()
        {
            // tolakram - when the brain stops, due to either death or no players in the vicinity, clear the aggro list
            if (!base.Stop())
                return false;

            ClearAggroList();
            return true;
        }

        public override void KillFSM()
        {
            FSM.KillFSM();
        }

        #region AI

        public override void Think()
        {
            FSM.Think();
        }

        public virtual bool CheckProximityAggro()
        {
            FireAmbientSentence();

            // Check aggro only if our aggro list is empty and we're not in combat.
            if (AggroLevel > 0 && AggroRange > 0 && Body.CurrentSpellHandler == null && !HasAggro && !IsWaitingForLosCheck)
            {
                CheckPlayerAggro();
                CheckNpcAggro();
            }

            // Some calls rely on this method to return if there's something in the aggro list, not necessarily to perform a proximity aggro check.
            // But this doesn't necessarily return whether or not the check was positive, only the current state (LoS checks take time).
            return HasAggro;
        }

        public virtual bool HasPatrolPath()
        {
            return Body.MaxSpeedBase > 0 &&
                Body.CurrentSpellHandler == null &&
                !Body.IsMoving &&
                !Body.attackComponent.AttackState &&
                !Body.InCombat &&
                !Body.IsMovingOnPath &&
                !string.IsNullOrEmpty(Body.PathID);
        }

        /// <summary>
        /// Check for aggro against players
        /// </summary>
        protected virtual void CheckPlayerAggro()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (!CanAggroTarget(player))
                    continue;

                if (player.IsStealthed || player.Steed != null)
                    continue;

                if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Shade))
                    continue;

                if (Properties.CHECK_LOS_BEFORE_AGGRO)
                    SendLosCheckForAggro(player, player);
                else
                {
                    AddToAggroList(player);
                    return;
                }

                // We don't know if the LoS check will be positive, so we have to ask other players
            }
        }

        /// <summary>
        /// Check for aggro against close NPCs
        /// </summary>
        protected virtual void CheckNpcAggro()
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort) AggroRange))
            {
                if (!CanAggroTarget(npc))
                    continue;

                if (npc is GameTaxi or GameTrainingDummy)
                    continue;

                if (Properties.CHECK_LOS_BEFORE_AGGRO)
                {
                    // Check LoS if either the target or the current mob is a pet
                    if (npc.Brain is ControlledMobBrain theirControlledNpcBrain && theirControlledNpcBrain.GetPlayerOwner() is GamePlayer theirOwner)
                    {
                        SendLosCheckForAggro(theirOwner, npc);
                        continue;
                    }
                    else if (this is ControlledMobBrain ourControlledNpcBrain && ourControlledNpcBrain.GetPlayerOwner() is GamePlayer ourOwner)
                    {
                        SendLosCheckForAggro(ourOwner, npc);
                        continue;
                    }
                }

                AddToAggroList(npc);
                return;
            }
        }

        public virtual void FireAmbientSentence()
        {
            if (Body.ambientTexts != null && Body.ambientTexts.Any(item => item.Trigger == "seeing"))
            {
                // Check if we can "see" players and fire off ambient text
                List<GamePlayer> currentPlayersSeen = GameLoop.GetListForTick<GamePlayer>();

                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
                {
                    if (!PlayersSeen.Contains(player))
                    {
                        Body.FireAmbientSentence(GameNPC.eAmbientTrigger.seeing, player);
                        PlayersSeen.Add(player);
                    }

                    currentPlayersSeen.Add(player);
                }

                for (int i = PlayersSeen.Count - 1; i >= 0; i--)
                {
                    if (!currentPlayersSeen.Contains(PlayersSeen[i]))
                        PlayersSeen.SwapRemoveAt(i);
                }
            }
        }

        /// <summary>
        /// The interval for thinking, min 1.5 seconds
        /// 10 seconds for 0 aggro mobs
        /// </summary>
        public override int ThinkInterval
        {
            get
            {
                if (Body is GameMerchant or GameTrainer or GameHastener)
                    return 5000; //Merchants and other special NPCs don't need to think that often

                return Math.Max(500, 1500 - (AggroLevel / 10 * 100));
            }
        }

        /// <summary>
        /// If this brain is part of a formation, it edits it's values accordingly.
        /// </summary>
        /// <param name="x">The x-coordinate to refer to and change</param>
        /// <param name="y">The x-coordinate to refer to and change</param>
        /// <param name="z">The x-coordinate to refer to and change</param>
        public virtual bool CheckFormation(ref int x, ref int y, ref int z)
        {
            return false;
        }

        /// <summary>
        /// Checks the Abilities
        /// </summary>
        public virtual void CheckAbilities()
        {
            //See CNPC
        }

        #endregion

        #region Aggro

        protected int _aggroRange;

        /// <summary>
        /// Max Aggro range in that this npc searches for enemies
        /// </summary>
        public virtual int AggroRange
        {
            get => Math.Min(_aggroRange, MAX_AGGRO_DISTANCE);
            set => _aggroRange = value;
        }

        /// <summary>
        /// Aggressive Level in % 0..100, 0 means not Aggressive
        /// </summary>
        public virtual int AggroLevel { get; set; }

        private ConcurrentDictionary<GameLiving, AggroAmount> _tempAggroList;
        protected ConcurrentDictionary<GameLiving, AggroAmount> AggroList { get; private set; } = new();
        protected List<OrderedAggroListElement> OrderedAggroList { get; private set; } = new();
        protected readonly Lock _orderedAggroListLock = new();
        public GameLiving LastHighestThreatInAttackRange { get; private set; }

        public class AggroAmount
        {
            public AggroAmount(long baseAggro = 0)
            {
                Base = baseAggro;
            }

            public long Base { get; set; }
            public long Effective { get; set; }
        }

        /// <summary>
        /// Checks whether living has someone on its aggrolist
        /// </summary>
        public virtual bool HasAggro => !AggroList.IsEmpty;

        /// <summary>
        /// Add aggro table of this brain to that of another living.
        /// </summary>
        public void AddAggroListTo(StandardMobBrain brain)
        {
            if (!brain.Body.IsAlive)
                return;

            foreach (var pair in AggroList)
                brain.AddToAggroList(pair.Key, pair.Value.Base);
        }

        public virtual void AddToAggroList(GameLiving living, long aggroAmount = 0)
        {
            if (Body.IsConfused || !Body.IsAlive || living == null)
                return;

            ForceAddToAggroList(living, aggroAmount);
        }

        public void ForceAddToAggroList(GameLiving living, long aggroAmount = 0)
        {
            if (aggroAmount > 0)
            {
                foreach (ProtectECSGameEffect protect in living.effectListComponent.GetAbilityEffects(eEffect.Protect))
                {
                    if (protect.Target != living)
                        continue;

                    GameLiving protectSource = protect.Source;

                    if (protectSource.IsIncapacitated || protectSource.IsSitting)
                        continue;

                    if (!living.IsWithinRadius(protectSource, ProtectAbilityHandler.PROTECT_DISTANCE))
                        continue;

                    // P I: prevents 10% of aggro amount
                    // P II: prevents 20% of aggro amount
                    // P III: prevents 30% of aggro amount
                    // guessed percentages, should never be higher than or equal to 50%
                    int abilityLevel = protectSource.GetAbilityLevel(Abilities.Protect);
                    long protectAmount = (long) (abilityLevel * 0.1 * aggroAmount);

                    if (protectAmount > 0)
                    {
                        aggroAmount -= protectAmount;

                        if (protectSource is GamePlayer playerProtectSource)
                        {
                            playerProtectSource.Out.SendMessage(LanguageMgr.GetTranslation(playerProtectSource.Client.Account.Language, "AI.Brain.StandardMobBrain.YouProtDist", living.GetName(0, false),
                                Body.GetName(0, false, playerProtectSource.Client.Account.Language, Body)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }

                        AggroList.AddOrUpdate(protectSource, Add, Update, protectAmount);
                    }
                }
            }

            AggroList.AddOrUpdate(living, Add, Update, aggroAmount);

            if (living is GamePlayer player)
            {
                AddPetAndSubPetsToAggroList(player);

                // Add the whole group to the aggro list.
                // This is done on every attack, but we may consider doing it only once per group, somehow.
                if (player.Group != null)
                {
                    foreach (GamePlayer playerInGroup in player.Group.GetPlayersInTheGroup())
                    {
                        if (playerInGroup != living)
                        {
                            if (!AggroList.ContainsKey(playerInGroup))
                                AggroList.TryAdd(playerInGroup, new(0));

                            AddPetAndSubPetsToAggroList(playerInGroup);
                        }
                    }
                }
            }

            // Change state and reschedule the next think tick to improve responsiveness.
            if (FSM.GetCurrentState() != FSM.GetState(eFSMStateType.AGGRO) && HasAggro)
            {
                FSM.SetCurrentState(eFSMStateType.AGGRO);
                NextThinkTick = GameLoop.GameLoopTime;
            }

            static AggroAmount Add(GameLiving key, long arg)
            {
                // Always add at least 1 if the key is not present to ensure the NPC goes to the puller and not a group member.
                // It's still technically possible for two group members to pull at the exact same time, but this should be fine.
                return new(Math.Max(1, arg));
            }

            static AggroAmount Update(GameLiving key, AggroAmount oldValue, long arg)
            {
                oldValue.Base = Math.Max(0, oldValue.Base + arg);
                return oldValue;
            }
        }

        private void AddPetAndSubPetsToAggroList(GamePlayer player)
        {
            GameNPC pet = player.ControlledBrain?.Body;

            if (pet == null)
                return;

            if (!AggroList.ContainsKey(pet))
                AggroList.TryAdd(pet, new(0));

            IControlledBrain[] controlledBrains = pet.ControlledNpcList;

            if (controlledBrains == null)
                return;

            foreach (IControlledBrain subPetBrain in controlledBrains)
            {
                if (subPetBrain == null)
                    continue;

                GameNPC subPet = subPetBrain.Body;

                if (subPet == null)
                    continue;

                if (!AggroList.ContainsKey(subPet))
                    AggroList.TryAdd(subPet, new(0));
            }
        }

        public virtual void RemoveFromAggroList(GameLiving living)
        {
            AggroList.TryRemove(living, out _);
        }

        public List<OrderedAggroListElement> GetOrderedAggroList()
        {
            // Potentially slow, so we cache the result.
            lock (_orderedAggroListLock)
            {
                if (OrderedAggroList.Count == 0)
                    OrderedAggroList = AggroList.OrderByDescending(x => x.Value.Effective).Select(x => new OrderedAggroListElement(x.Key, x.Value.Effective)).ToList();

                return OrderedAggroList.ToList();
            }
        }

        public long GetBaseAggroAmount(GameLiving living)
        {
            return AggroList.TryGetValue(living, out AggroAmount aggroAmount) ? aggroAmount.Base : 0;
        }

        public bool SetTemporaryAggroList()
        {
            if (_tempAggroList != null)
                return false;

            _tempAggroList = AggroList;
            AggroList = new();
            return true;
        }

        public bool UnsetTemporaryAggroList()
        {
            if (_tempAggroList == null)
                return false;

            AggroList = _tempAggroList;
            _tempAggroList = null;

            if (HasAggro)
            {
                if (FSM.GetCurrentState() != FSM.GetState(eFSMStateType.AGGRO))
                    FSM.SetCurrentState(eFSMStateType.AGGRO);

                NextThinkTick = GameLoop.GameLoopTime;
            }

            return true;
        }

        /// <summary>
        /// Remove all livings from the aggrolist.
        /// </summary>
        public virtual void ClearAggroList()
        {
            CanBaf = true; // Mobs that drop out of combat can BAF again.
            AggroList.Clear();

            lock (_orderedAggroListLock)
            {
                OrderedAggroList.Clear();
            }

            LastHighestThreatInAttackRange = null;
        }

        /// <summary>
        /// Selects and attacks the next target or does nothing.
        /// </summary>
        public virtual void AttackMostWanted()
        {
            if (!IsActive)
                return;

            GameLiving newTarget = CalculateNextAttackTarget();

            if (newTarget == null)
                return;

            if (Body.TargetObject == null)
            {
                BringFriends(newTarget);
                Body.FireAmbientSentence(GameNPC.eAmbientTrigger.aggroing, newTarget);

                // Don't immediately cast instant harmful spells. It's annoying.
                if (Body.CanCastInstantHarmfulSpells && !Body.IsInstantHarmfulSpellCastingLocked)
                    Body.ApplyInstantHarmfulSpellDelay();
            }

            Body.TargetObject = newTarget;

            if (CheckSpells(eCheckSpellType.Offensive))
                Body.StopAttack();
            else if (!Body.IsCasting)
                Body.StartAttack(newTarget);
        }

        public virtual void Disengage()
        {
            ClearAggroList();
            Body.StopAttack();
            Body.StopCurrentSpellcast();
            Body.TargetObject = null;
        }

        private int _pendingLosCheckCount;
        public int PendingLosCheckCount => _pendingLosCheckCount;
        public bool IsWaitingForLosCheck => Interlocked.CompareExchange(ref _pendingLosCheckCount, 0, 0) > 0;
        protected virtual bool CanAddToAggroListFromMultipleLosChecks => false;

        protected void SendLosCheckForAggro(GamePlayer player, GameObject target)
        {
            if (player.Out.SendCheckLos(Body, target, new CheckLosResponse(LosCheckForAggroCallback)))
                Interlocked.Increment(ref _pendingLosCheckCount);
        }

        protected void LosCheckForAggroCallback(GamePlayer player, LosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            // This method should not be allowed to be executed at the same time as `CheckPlayerAggro` or `CheckNPCAggro`.
            if (response is LosCheckResponse.True)
            {
                if (!HasAggro || CanAddToAggroListFromMultipleLosChecks)
                {
                    GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

                    if (gameObject is GameLiving gameLiving)
                        AddToAggroList(gameLiving);
                }
            }

            Interlocked.Decrement(ref _pendingLosCheckCount);
        }

        protected virtual bool ShouldBeRemovedFromAggroList(GameLiving living)
        {
            // Keep Necromancer shades so that we can attack them if their pets die.
            return !living.IsAlive ||
                   living.CurrentRegion != Body.CurrentRegion ||
                   (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true) && !living.effectListComponent.ContainsEffectForEffectType(eEffect.Shade));
        }

        protected virtual bool ShouldBeIgnoredFromAggroList(GameLiving living)
        {
            // We're keeping shades in the aggro list so that mobs attack them after their pet dies, so they need to be filtered out here.
            // We also keep entities outside MAX_AGGRO_LIST_DISTANCE in case they come back.
            return living.effectListComponent.ContainsEffectForEffectType(eEffect.Shade) || !Body.IsWithinRadius(living, MAX_AGGRO_LIST_DISTANCE);
        }

        protected virtual GameLiving CleanUpAggroListAndGetHighestModifiedThreat()
        {
            // Clear cached ordered aggro list.
            // It isn't built here because ordering all entities in the aggro list can be expensive, and we typically don't need it.
            // It's built on demand, when `GetOrderedAggroList` is called.
            OrderedAggroList.Clear();
            int attackRange = Body.attackComponent.AttackRange;
            GameLiving highestThreat = null;
            KeyValuePair<GameLiving, AggroAmount> currentTarget = default;
            long highestEffectiveAggro = -1; // Assumes that negative aggro amounts aren't allowed in the list.
            long highestEffectiveAggroInAttackRange = -1; // Assumes that negative aggro amounts aren't allowed in the list.

            foreach (var pair in AggroList)
            {
                GameLiving living = pair.Key;

                if (Body.TargetObject == living)
                    currentTarget = pair;

                if (ShouldBeRemovedFromAggroList(living))
                {
                    AggroList.TryRemove(living, out _);
                    continue;
                }

                if (ShouldBeIgnoredFromAggroList(living))
                    continue;

                // Livings further than `EFFECTIVE_AGGRO_AMOUNT_CALCULATION_DISTANCE_THRESHOLD` units away have a reduced effective aggro amount.
                // Using `Math.Ceiling` helps differentiate between 0 and 1 base aggro amount.
                AggroAmount aggroAmount = pair.Value;
                double distance = Body.GetDistanceTo(living);
                double distanceOverThreshold = distance - EFFECTIVE_AGGRO_DISTANCE_THRESHOLD;

                if (distanceOverThreshold <= 0)
                    aggroAmount.Effective = aggroAmount.Base;
                else
                    aggroAmount.Effective = (long) Math.Ceiling(aggroAmount.Base * Math.Exp(EFFECTIVE_AGGRO_EXPONENT * distanceOverThreshold));

                if (aggroAmount.Effective > highestEffectiveAggroInAttackRange)
                {
                    if (distance <= attackRange)
                    {
                        highestEffectiveAggroInAttackRange = aggroAmount.Effective;
                        LastHighestThreatInAttackRange = living;
                    }

                    if (aggroAmount.Effective > highestEffectiveAggro)
                    {
                        highestEffectiveAggro = aggroAmount.Effective;
                        highestThreat = living;
                    }
                }
            }

            if (highestThreat != null)
            {
                // Don't change target if our new found highest threat has the same effective aggro.
                // This helps with BAF code to make mobs actually go to their intended target.
                if (currentTarget.Key != null && currentTarget.Key != highestThreat && currentTarget.Value.Effective >= highestEffectiveAggro)
                    highestThreat = currentTarget.Key;
            }
            else
            {
                // The list seems to be full of shades. It could mean we added a shade to the aggro list instead of its pet.
                // Ideally, this should never happen, but it currently can be caused by the way `AddToAggroList` propagates aggro to group members.
                // When that happens, don't bother checking aggro amount and simply return the first pet in the list.
                return AggroList.FirstOrDefault().Key?.ControlledBrain?.Body;
            }

            return highestThreat;
        }

        /// <summary>
        /// Returns the best target to attack from the current aggro list.
        /// </summary>
        protected virtual GameLiving CalculateNextAttackTarget()
        {
            return CleanUpAggroListAndGetHighestModifiedThreat();
        }

        public virtual bool CanAggroTarget(GameLiving target)
        {
            if (!GameServer.ServerRules.IsAllowedToAttack(Body, target, true))
                return false;

            // Get owner if target is pet or subpet
            GameLiving realTarget = target;

            if (realTarget is GameNPC npcTarget && npcTarget.Brain is IControlledBrain npcTargetBrain)
            {
                GamePlayer realTargetOwner = npcTargetBrain.GetPlayerOwner();

                if (realTargetOwner != null)
                    realTarget = realTargetOwner;
            }

            // Only attack if green+ to target
            if (realTarget.IsObjectGreyCon(Body))
                return false;

            // If this npc have Faction return the AggroAmount to Player
            if (Body.Faction != null)
            {
                if (realTarget is GamePlayer realTargetPlayer)
                    return Body.Faction.GetStandingToFaction(realTargetPlayer) is Faction.Standing.AGGRESIVE;
                else if (realTarget is GameNPC realTargetNpc && Body.Faction.EnemyFactions.Contains(realTargetNpc.Faction))
                    return true;
            }

            // We put this here to prevent aggroing non-factions npcs
            return (Body.Realm != eRealm.None || realTarget is not GameNPC) && AggroLevel > 0;
        }

        public virtual void OnAttackedByEnemy(AttackData ad)
        {
            ConvertAttackToAggroAmount(ad);
        }

        /// <summary>
        /// Converts an amount into an aggro amount, and splits it between the pet and its owner if necessary.
        /// </summary>
        protected void ConvertAttackToAggroAmount(AttackData ad)
        {
            if (!ad.GeneratesAggro || !Body.IsAlive || Body.ObjectState is not GameObject.eObjectState.Active || FSM.GetCurrentState() == FSM.GetState(eFSMStateType.PASSIVE))
                return;

            int damage = Math.Max(0, ad.Damage + ad.CriticalDamage);
            GameLiving attacker = ad.Attacker;

            if (attacker is GameNPC NpcAttacker && NpcAttacker.Brain is ControlledMobBrain controlledBrain)
            {
                damage = controlledBrain.ModifyDamageWithTaunt(damage);

                // A pet generates 100% of the aggro from its damage; the owner receives 30% additional aggro as a tag, without reducing the pet's contribution.
                // The pet should be added first to the aggro list in case the attack does no damage (see `AddToAggroList` implementation).
                AddToAggroList(NpcAttacker, damage);
                AddToAggroList(controlledBrain.Owner, (int) (damage * 0.3));
            }
            else
                AddToAggroList(attacker, damage);
        }

        #endregion

        #region Bring a Friend

        private bool _canBaf = true;

        public int BafAddCount { get; private set; } // Used for experience bonus. Reset anytime `CanBaf` is modified.
        protected static ushort BAF_MIN_RADIUS => 450; // BaF radius for a solo player (assuming solo players are allowed to trigger BaF).
        protected static ushort BAF_EXTRA_RADIUS_PER_OTHER_PLAYER => 150; // Caps at 8 players.
        protected static double BAF_RADIUS_DUNGEON_MODIFIER => 0.5;

        public virtual bool CanBaf
        {
            // Prevent NPCs that were charmed from initiating a BaF or replying to one.
            get => _canBaf && GameLoop.GameLoopTime - GameNPC.CHARMED_NOEXP_TIMEOUT >= Body.TempProperties.GetProperty<long>(GameNPC.CHARMED_TICK_PROP);
            set
            {
                _canBaf = value;
                BafAddCount = 0;
            }
        }

        protected virtual void BringFriends(GameLiving puller)
        {
            // BaF only happens once.
            if (!CanBaf)
                return;

            // BaF only happens if the NPC has a faction, and if it was attacked (no BAF on body pull).
            if (Body.Faction == null || Body.attackComponent.AttackerTracker.Count == 0)
            {
                _canBaf = false;
                return;
            }

            GamePlayer playerPuller;

            // Only BAF on players and pets of players
            if (puller is GamePlayer player)
                playerPuller = player;
            else if (puller is GameNPC pet && pet.Brain is ControlledMobBrain brain)
            {
                playerPuller = brain.GetPlayerOwner();

                if (playerPuller == null)
                    return;
            }
            else
                return;

            // Prevent the same puller from triggering two BAFs at the same time.
            if (!playerPuller.TempProperties.TrySetProperty(ResetBafPropertyAction.Property, true))
                 return;

            _ = new ResetBafPropertyAction(playerPuller);
            CanBaf = false; // Mobs only BAF once per fight.
            int maxAdds = GetMaxAddsCountFromBaf(playerPuller, out List<GamePlayer> otherTargets, out int attackersCount);
            int bafRadius = BAF_MIN_RADIUS + (Math.Min(8, attackersCount) - 1) * BAF_EXTRA_RADIUS_PER_OTHER_PLAYER;

            if (Body.CurrentZone.IsDungeon)
                bafRadius = (int) (bafRadius * BAF_RADIUS_DUNGEON_MODIFIER);

            List<GameNPC> brainsInRadius = GetFriendlyAndAvailableNpcsInRadiusOrderedByDistance(bafRadius, maxAdds);
            int addCount = brainsInRadius.Count;
            BafAddCount = addCount;

            foreach (GameNPC npc in brainsInRadius)
            {
                StandardMobBrain brain = npc.Brain as StandardMobBrain; // Guaranteed by GetFriendlyAndAvailableNpcsInRadiusOrderedByDistance.
                brain.CanBaf = false;
                brain.BafAddCount = addCount;
                GameLiving target;

                if (otherTargets != null && otherTargets.Count > 1)
                    target = otherTargets[Util.Random(0, otherTargets.Count - 1)];
                else
                    target = puller;

                brain.AddToAggroList(target);
            }

            static int GetMaxAddsCountFromBaf(GamePlayer puller, out List<GamePlayer> otherTargets, out int attackersCount)
            {
                attackersCount = 0;
                otherTargets = null;
                HashSet<string> countedVictims = null;
                HashSet<string> countedAttackers = null;
                BattleGroup bg = puller.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
                Group group = puller.Group;

                if (group != null)
                {
                    if (Properties.BAF_MOBS_COUNT_BG_MEMBERS && bg != null)
                        countedAttackers = [];

                    if (!Properties.BAF_MOBS_ATTACK_PULLER)
                    {
                        if (Properties.BAF_MOBS_ATTACK_BG_MEMBERS && bg != null)
                        {
                            otherTargets = new(group.MemberCount + bg.PlayerCount - 1);
                            countedVictims = [];
                        }
                        else
                            otherTargets = new(group.MemberCount);
                    }

                    foreach (GamePlayer playerInGroup in group.GetPlayersInTheGroup())
                    {
                        if (playerInGroup != null && (playerInGroup.InternalID == puller.InternalID || playerInGroup.IsWithinRadius(puller, WorldMgr.VISIBILITY_DISTANCE, true)))
                        {
                            attackersCount++;
                            countedAttackers?.Add(playerInGroup.InternalID);

                            if (otherTargets != null)
                            {
                                otherTargets.Add(playerInGroup);
                                countedVictims?.Add(playerInGroup.InternalID);
                            }
                        }
                    }
                }

                if (bg != null && (Properties.BAF_MOBS_COUNT_BG_MEMBERS || (Properties.BAF_MOBS_ATTACK_BG_MEMBERS && !Properties.BAF_MOBS_ATTACK_PULLER)))
                {
                    if (otherTargets == null && Properties.BAF_MOBS_ATTACK_BG_MEMBERS && !Properties.BAF_MOBS_ATTACK_PULLER)
                        otherTargets = new(bg.PlayerCount);

                    foreach (GamePlayer player2 in bg.Members.Keys)
                    {
                        if (player2 != null && (player2.InternalID == puller.InternalID || player2.IsWithinRadius(puller, WorldMgr.VISIBILITY_DISTANCE, true)))
                        {
                            if (Properties.BAF_MOBS_COUNT_BG_MEMBERS && (countedAttackers == null || !countedAttackers.Contains(player2.InternalID)))
                                attackersCount++;

                            if (otherTargets != null && (countedVictims == null || !countedVictims.Contains(player2.InternalID)))
                                otherTargets.Add(player2);
                        }
                    }
                }

                // Player is alone.
                if (attackersCount == 0)
                    attackersCount = 1;

                int percentBAF = Properties.BAF_INITIAL_CHANCE + (attackersCount - 1) * Properties.BAF_ADDITIONAL_CHANCE;
                int maxAdds = percentBAF / 100; // Multiple of 100 are guaranteed BAFs.

                // Calculate chance of an addition add based on the remainder.
                if (Util.Chance(percentBAF % 100))
                    maxAdds++;

                return Math.Max(0, maxAdds);
            }
        }

        public List<GameNPC> GetFriendlyAndAvailableNpcsInRadiusOrderedByDistance(int radius, int count)
        {
            List<GameNPC> finalNpcs = GameLoop.GetListForTick<GameNPC>();

            if (count <= 0)
                return finalNpcs;

            NpcWithDistance[] candidates = ArrayPool<NpcWithDistance>.Shared.Rent(count);

            try
            {
                int candidateCount = 0;

                foreach (GameNPC npc in Body.GetNPCsInRadius((ushort) radius))
                {
                    if (npc == Body || !npc.IsFriend(Body) || !npc.IsAggressive || !npc.IsAvailableToJoinFight || npc.Brain is not StandardMobBrain)
                        continue;

                    long xDiff = Body.X - npc.X;
                    long yDiff = Body.Y - npc.Y;
                    long zDiff = Body.Z - npc.Z;
                    long distSq = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;

                    if (candidateCount < count)
                    {
                        candidates[candidateCount++] = new(npc, distSq);

                        // Build the heap when we have enough candidates.
                        if (candidateCount == count)
                        {
                            for (int i = count / 2 - 1; i >= 0; i--)
                                HeapifyDown(candidates, i, count);
                        }
                    }
                    else if (distSq < candidates[0].DistanceSq)
                    {
                        candidates[0] = new(npc, distSq);
                        HeapifyDown(candidates, 0, count);
                    }
                }

                if (candidateCount > 0)
                {
                    Array.Sort(candidates, 0, candidateCount, NpcDistanceComparer.Instance);

                    for (int i = 0; i < candidateCount; i++)
                        finalNpcs.Add(candidates[i].Npc);
                }

                return finalNpcs;
            }
            finally
            {
                ArrayPool<NpcWithDistance>.Shared.Return(candidates);
            }

            static void HeapifyDown(NpcWithDistance[] heap, int index, int heapSize)
            {
                int largest = index;

                while (true)
                {
                    int currentIndex = largest;
                    int left = 2 * currentIndex + 1;
                    int right = 2 * currentIndex + 2;

                    if (left < heapSize && heap[left].DistanceSq > heap[largest].DistanceSq)
                        largest = left;

                    if (right < heapSize && heap[right].DistanceSq > heap[largest].DistanceSq)
                        largest = right;

                    if (largest == currentIndex)
                        break;

                    (heap[currentIndex], heap[largest]) = (heap[largest], heap[currentIndex]);
                }
            }
        }

        private readonly struct NpcWithDistance
        {
            public readonly GameNPC Npc;
            public readonly long DistanceSq;

            public NpcWithDistance(GameNPC npc, long distanceSq)
            {
                Npc = npc;
                DistanceSq = distanceSq;
            }
        }

        private class NpcDistanceComparer : IComparer<NpcWithDistance>
        {
            public static readonly NpcDistanceComparer Instance = new();

            public int Compare(NpcWithDistance a, NpcWithDistance b)
            {
                return a.DistanceSq.CompareTo(b.DistanceSq);
            }
        }

        private class ResetBafPropertyAction: ECSGameTimerWrapperBase
        {
            public const string Property = "IsTriggeringBaf";

            public ResetBafPropertyAction(GameObject owner) : base(owner)
            {
                Start(0);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                (Owner as GameLiving).TempProperties.RemoveProperty(Property);
                return 0;
            }
        }

        #endregion

        #region Spells

        public enum eCheckSpellType
        {
            Offensive,
            Defensive
        }

        public virtual bool CheckSpells(eCheckSpellType type)
        {
            if (Body == null || Body.Spells == null || Body.Spells.Count == 0)
                return false;

            // Becomes true when:
            // * The NPC is currently casting or is about to cast a spell.
            // * The spell is waiting for LoS check.
            // * The spell is blocked by self-interrupt.
            // In all these cases, the NPC should not start a melee attack.
            bool isCastingOrWantsToCast = false;

            if (type is eCheckSpellType.Defensive)
            {
                if (Body.CanCastInstantHealSpells)
                    CheckDefensiveSpells(Body.InstantHealSpells);

                if (Body.CanCastInstantMiscSpells)
                    CheckDefensiveSpells(Body.InstantMiscSpells);

                if (Body.CanCastHealSpells)
                    isCastingOrWantsToCast = CheckDefensiveSpells(Body.HealSpells);

                if (!isCastingOrWantsToCast && Body.CanCastMiscSpells)
                    isCastingOrWantsToCast = CheckDefensiveSpells(Body.MiscSpells);
            }
            else if (type is eCheckSpellType.Offensive)
            {
                // Instant harmful spells have a random global cooldown, set by the casting component.
                if (Body.CanCastInstantHarmfulSpells && !Body.IsInstantHarmfulSpellCastingLocked)
                    CheckOffensiveSpells(Body.InstantHarmfulSpells);

                if (Body.CanCastHarmfulSpells)
                    isCastingOrWantsToCast = CheckOffensiveSpells(Body.HarmfulSpells);
            }

            return isCastingOrWantsToCast || Body.IsCasting;
        }

        private bool CheckOffensiveSpells(List<Spell> spells)
        {
            bool blockedBySelfInterrupt = false;
            Spell spellToCast = null;
            int castableCount = 0;

            foreach (Spell spell in spells)
            {
                if (!CanCastOffensiveSpell(spell, out bool selfInterrupt))
                {
                    if (selfInterrupt)
                        blockedBySelfInterrupt = true;

                    continue;
                }

                // With each valid spell we find, we give it a 1-in-N chance to become the chosen one.
                if (Util.Random(castableCount) == 0)
                    spellToCast = spell;

                castableCount++;
            }

            if (spellToCast == null)
                return blockedBySelfInterrupt;

            return Body.CastSpell(spellToCast, m_mobSpellLine);
        }

        private bool CanCastOffensiveSpell(Spell spell, out bool blockedBySelfInterrupt)
        {
            bool isInterruptible = spell.CastTime > 0 && !spell.Uninterruptible;
            blockedBySelfInterrupt = false;

            if (isInterruptible && Body.IsBeingInterruptedByOther)
                return false;

            if (spell.HasRecastDelay && Body.GetSkillDisabledDuration(spell) > 0)
                return false;

            if (Body.TargetObject is not GameLiving target || !Body.IsWithinRadius(target, spell.CalculateEffectiveRange(Body)))
                return false;

            if (spell.Duration > 0 || spell.IsConcentration)
            {
                if (spell.SpellType is not eSpellType.DirectDamageWithDebuff and not eSpellType.DamageSpeedDecrease && LivingHasEffect(target, spell))
                    return false;
            }

            if (isInterruptible && Body.IsBeingSelfInterrupted)
            {
                blockedBySelfInterrupt = true;
                return false;
            }

            return true;
        }

        private bool CheckDefensiveSpells(List<Spell> spells)
        {
            bool blockedBySelfInterrupt = false;
            (Spell spell, GameLiving target) candidate = (null, null);
            int validSpellsFound = 0;

            foreach (Spell spell in spells)
            {
                if (!CanCastDefensiveSpell(spell, out GameLiving target, out bool selfInterrupt))
                {
                    if (selfInterrupt)
                        blockedBySelfInterrupt = true;

                    continue;
                }

                // Reservoir Sampling for a single item (k=1).
                // The first valid spell is always chosen.
                // The second has a 1/2 chance of replacing the first.
                // The third has a 1/3 chance of replacing the current candidate, and so on.
                if (Util.Random(validSpellsFound) == 0)
                    candidate = (spell, target);

                validSpellsFound++;
            }

            if (validSpellsFound == 0)
                return blockedBySelfInterrupt;

            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = candidate.target;
            bool cast = Body.CastSpell(candidate.spell, m_mobSpellLine);
            Body.TargetObject = oldTarget;
            return cast;
        }

        private bool CanCastDefensiveSpell(Spell spell, out GameLiving target, out bool blockedBySelfInterrupt)
        {
            bool isInterruptible = spell.CastTime > 0 && !spell.Uninterruptible;
            target = null;
            blockedBySelfInterrupt = false;

            if (isInterruptible && Body.IsBeingInterruptedByOther)
                return false;

            if (spell.HasRecastDelay && Body.GetSkillDisabledDuration(spell) > 0)
                return false;

            target = FindTargetForDefensiveSpell(spell);

            if (target == null)
                return false;

            if (isInterruptible && Body.IsBeingSelfInterrupted)
            {
                blockedBySelfInterrupt = true;
                return false;
            }

            return true;
        }

        protected virtual GameLiving FindTargetForDefensiveSpell(Spell spell)
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
                    if (!LivingHasEffect(Body, spell) && !Body.attackComponent.AttackState && spell.Target is not eSpellTarget.PET)
                    {
                        target = Body;
                        break;
                    }

                    if (Body.ControlledBrain?.Body != null &&
                        spell.Target is not eSpellTarget.SELF &&
                        Body.IsWithinRadius(target, spell.CalculateEffectiveRange(Body)) &&
                        !LivingHasEffect(Body.ControlledBrain.Body, spell))
                    {
                        target = Body.ControlledBrain.Body;
                        break;
                    }

                    break;
                }

                #endregion Buffs

                #region Disease Cure/Poison Cure/Summon

                case eSpellType.CureDisease:
                {
                    if (Body.IsDiseased)
                    {
                        target = Body;
                        break;
                    }

                    if (Body.ControlledBrain?.Body != null &&
                        Body.ControlledBrain.Body.IsDiseased &&
                        spell.Target is not eSpellTarget.SELF &&
                        Body.IsWithinRadius(Body.ControlledBrain.Body, spell.CalculateEffectiveRange(Body)))
                    {
                        target = Body.ControlledBrain.Body;
                        break;
                    }

                    break;
                }
                case eSpellType.CurePoison:
                {
                    if (Body.IsPoisoned)
                    {
                        target = Body;
                        break;
                    }

                    if (Body.ControlledBrain?.Body != null &&
                        Body.ControlledBrain.Body.IsPoisoned &&
                        spell.Target is not eSpellTarget.SELF &&
                        Body.IsWithinRadius(Body.ControlledBrain.Body, spell.CalculateEffectiveRange(Body)))
                    {
                        target = Body.ControlledBrain.Body;
                        break;
                    }

                    break;
                }
                case eSpellType.Summon:
                {
                    target = Body;
                    break;
                }
                case eSpellType.SummonMinion:
                {
                    // If the list is null, lets make sure it gets initialized.
                    if (Body.ControlledNpcList == null)
                        Body.InitControlledBrainArray(2);
                    else
                    {
                        // Let's check to see if the list is full - if it is, we can't cast another minion.
                        // If it isn't, let them cast.
                        IControlledBrain[] icb = Body.ControlledNpcList;
                        int numberOfPets = 0;

                        for (int i = 0; i < icb.Length; i++)
                        {
                            if (icb[i] != null)
                                numberOfPets++;
                        }

                        if (numberOfPets >= icb.Length)
                            break;
                    }

                    target = Body;
                    break;
                }

                #endregion Disease Cure/Poison Cure/Summon

                #region Heals

                case eSpellType.CombatHeal:
                case eSpellType.Heal:
                case eSpellType.HealOverTime:
                case eSpellType.MercHeal:
                case eSpellType.OmniHeal:
                case eSpellType.PBAoEHeal:
                case eSpellType.SpreadHeal:
                {
                    if (Body.HealthPercent < Properties.NPC_HEAL_THRESHOLD)
                    {
                        target = Body;
                        break;
                    }

                    if (Body.ControlledBrain?.Body != null &&
                        spell.Target is not eSpellTarget.SELF &&
                        Body.IsWithinRadius(Body.ControlledBrain.Body, spell.CalculateEffectiveRange(Body)) &&
                        Body.ControlledBrain.Body.HealthPercent < Properties.NPC_HEAL_THRESHOLD)
                    {
                        target = Body.ControlledBrain.Body;
                        break;
                    }

                    break;
                }

                #endregion

                case eSpellType.SummonCommander:
                case eSpellType.SummonDruidPet:
                case eSpellType.SummonHunterPet:
                case eSpellType.SummonNecroPet:
                case eSpellType.SummonUnderhill:
                case eSpellType.SummonSimulacrum:
                case eSpellType.SummonSpiritFighter:
                {
                    if (Body.ControlledBrain != null)
                        break;

                    target = Body;
                    break;
                }
                default:
                    break;
            }

            return target;
        }

        protected static SpellLine m_mobSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);

        /// <summary>
        /// Checks if the living target has a spell effect.
        /// Only to be used for spell casting purposes.
        /// </summary>
        /// <returns>True if the living has the effect of will receive it by our current spell.</returns>
        public bool LivingHasEffect(GameLiving target, Spell spell)
        {
            if (target == null)
                return true;

            eEffect spellEffect = EffectHelper.GetEffectFromSpell(spell);

            // Ignore effects that aren't actually effects (may be incomplete).
            if (spellEffect is eEffect.DirectDamage or eEffect.Pet or eEffect.Unknown)
                return false;

            /* all my homies hate vampires
            if (target is GamePlayer && (target as GamePlayer).CharacterClass.ID == (int)eCharacterClass.Vampiir)
            {
                switch (spell.SpellType)
                {
                    case eSpellType.StrengthConstitutionBuff:
                    case eSpellType.DexterityQuicknessBuff:
                    case eSpellType.StrengthBuff:
                    case eSpellType.DexterityBuff:
                    case eSpellType.ConstitutionBuff:
                    case eSpellType.AcuityBuff:
                        return true;
                }
            }*/

            SpellHandler spellHandler = Body.castingComponent.SpellHandler;

            // If we're currently casting 'spell' on 'target', assume it already has the effect.
            // This allows spell queuing while preventing casting on the same target more than once.
            if (spellHandler != null && spellHandler.Spell.ID == spell.ID && spellHandler.Target == target)
                return true;

            SpellHandler queuedSpellHandler = Body.castingComponent.QueuedSpellHandler;

            // Do the same for our queued up spell.
            // This can happen on charmed pets having two buffs that they're trying to cast on their owner.
            if (queuedSpellHandler != null && queuedSpellHandler.Spell.ID == spell.ID && queuedSpellHandler.Target == target)
                return true;

            // May not be the right place for that, but without that check NPCs with more than one offensive or defensive proc will only buff themselves once.
            if (spell.SpellType is eSpellType.OffensiveProc or eSpellType.DefensiveProc)
            {
                List<ECSGameSpellEffect> existingEffects = target.effectListComponent.GetSpellEffects(spellEffect);

                foreach (ECSGameSpellEffect effect in existingEffects)
                {
                    if (effect.SpellHandler.Spell.ID == spell.ID || (spell.EffectGroup > 0 && effect.SpellHandler.Spell.EffectGroup == spell.EffectGroup))
                        return true;
                }

                return false;
            }

            // True if the target has the effect, or the immunity effect for this effect.
            // Treat NPC immunity effects as full immunity effects.
            return EffectListService.GetEffectOnTarget(target, spellEffect) != null ||
                HasImmunityEffect(target, EffectHelper.GetImmunityEffectFromSpell(spell)) ||
                HasImmunityEffect(target, EffectHelper.GetNpcImmunityEffectFromSpell(spell));

            static bool HasImmunityEffect(GameLiving target, eEffect immunityEffect)
            {
                return immunityEffect is not eEffect.Unknown && EffectListService.GetEffectOnTarget(target, immunityEffect) != null;
            }
        }

        #endregion

        public class OrderedAggroListElement
        {
            public GameLiving Living { get; }
            public long AggroAmount { get; }

            public OrderedAggroListElement(GameLiving living, long aggroAmount)
            {
                Living = living;
                AggroAmount = aggroAmount;
            }
        }
    }
}
