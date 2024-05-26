using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.GS;
using DOL.GS.Keeps;
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
        private const int EFFECTIVE_AGGRO_AMOUNT_CALCULATION_DISTANCE_THRESHOLD = 500;

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
            FSM.Add(new StandardMobState_DEAD(this));
            FSM.SetCurrentState(eFSMStateType.WAKING_UP);
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
            if (base.Stop())
            {
                ClearAggroList();
                return true;
            }

            return false;
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
            if (AggroLevel > 0 && AggroRange > 0 && !HasAggro && Body.CurrentSpellHandler == null)
            {
                CheckPlayerAggro();
                CheckNPCAggro();
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
                    // We don't know if the LoS check will be positive, so we have to ask other players
                    player.Out.SendCheckLos(Body, player, new CheckLosResponse(LosCheckForAggroCallback));
                else
                {
                    AddToAggroList(player, 1);
                    return;
                }
            }
        }

        /// <summary>
        /// Check for aggro against close NPCs
        /// </summary>
        protected virtual void CheckNPCAggro()
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
                        theirOwner.Out.SendCheckLos(Body, npc, new CheckLosResponse(LosCheckForAggroCallback));
                        continue;
                    }
                    else if (this is ControlledMobBrain ourControlledNpcBrain && ourControlledNpcBrain.GetPlayerOwner() is GamePlayer ourOwner)
                    {
                        ourOwner.Out.SendCheckLos(Body, npc, new CheckLosResponse(LosCheckForAggroCallback));
                        continue;
                    }
                }

                AddToAggroList(npc, 1);
                return;
            }
        }

        public virtual void FireAmbientSentence()
        {
            if (Body.ambientTexts != null && Body.ambientTexts.Any(item => item.Trigger == "seeing"))
            {
                // Check if we can "see" players and fire off ambient text
                List<GamePlayer> currentPlayersSeen = new();

                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
                {
                    if (!PlayersSeen.Contains(player))
                    {
                        Body.FireAmbientSentence(GameNPC.eAmbientTrigger.seeing, player);
                        PlayersSeen.Add(player);
                    }

                    currentPlayersSeen.Add(player);
                }

                for (int i = 0; i < PlayersSeen.Count; i++)
                {
                    if (!currentPlayersSeen.Contains(PlayersSeen[i]))
                        PlayersSeen.RemoveAt(i);
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

        protected ConcurrentDictionary<GameLiving, AggroAmount> AggroList { get; } = new();
        protected List<(GameLiving, long)> OrderedAggroList { get; private set; } = [];
        public GameLiving LastHighestThreatInAttackRange { get; private set; }

        public class AggroAmount(long @base = 0)
        {
            public long Base { get; set; } = @base;
            public long Effective { get; set; }
            public long Temporary { get; set; }
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

        public virtual void AddToAggroList(GameLiving living, long aggroAmount)
        {
            if (Body.IsConfused || !Body.IsAlive || living == null)
                return;

            if (aggroAmount > 0)
            {
                foreach (ProtectECSGameEffect protect in living.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == eEffect.Protect))
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
                // Add the whole group to the aggro list.
                if (player.Group != null)
                {
                    foreach (GamePlayer playerInGroup in player.Group.GetPlayersInTheGroup())
                    {
                        if (playerInGroup != living)
                            AggroList.TryAdd(playerInGroup, new());
                    }
                }
            }

            static AggroAmount Add(GameLiving key, long arg)
            {
                return new(Math.Max(0, arg));
            }

            static AggroAmount Update(GameLiving key, AggroAmount oldValue, long arg)
            {
                oldValue.Base = Math.Max(0, oldValue.Base + arg);
                return oldValue;
            }
        }

        public virtual void RemoveFromAggroList(GameLiving living)
        {
            AggroList.TryRemove(living, out _);
        }

        public List<(GameLiving, long)> GetOrderedAggroList()
        {
            // Potentially slow, so we cache the result.
            lock (((ICollection) OrderedAggroList).SyncRoot)
            {
                if (!OrderedAggroList.Any())
                    OrderedAggroList = AggroList.OrderByDescending(x => x.Value.Effective).Select(x => (x.Key, x.Value.Effective)).ToList();

                return OrderedAggroList.ToList();
            }
        }

        public long GetBaseAggroAmount(GameLiving living)
        {
            return AggroList.TryGetValue(living, out AggroAmount aggroAmount) ? aggroAmount.Base : 0;
        }

        /// <summary>
        /// Remove all livings from the aggrolist.
        /// </summary>
        public virtual void ClearAggroList()
        {
            CanBAF = true; // Mobs that drop out of combat can BAF again.
            AggroList.Clear();

            lock (((ICollection) OrderedAggroList).SyncRoot)
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
            }

            Body.TargetObject = newTarget;

            if (CheckSpells(eCheckSpellType.Offensive))
                Body.StopAttack();
            else
                Body.StartAttack(newTarget);
        }

        private long _isHandlingAdditionToAggroListFromLosCheck;
        private bool StartAddToAggroListFromLosCheck => Interlocked.Exchange(ref _isHandlingAdditionToAggroListFromLosCheck, 1) == 0; // Returns true the first time it's called.

        protected virtual void LosCheckForAggroCallback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            // Make sure only one thread can enter this block to prevent multiple entities from being added to the aggro list.
            // Otherwise mobs could kill one player and immediately go for another one.
            if (response is eLosCheckResponse.TRUE && StartAddToAggroListFromLosCheck)
            {
                if (!HasAggro)
                {
                    GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

                    if (gameObject is GameLiving gameLiving)
                        AddToAggroList(gameLiving, 1);
                }

                _isHandlingAdditionToAggroListFromLosCheck = 0;
            }
        }

        protected virtual bool ShouldBeRemovedFromAggroList(GameLiving living)
        {
            // Keep Necromancer shades so that we can attack them if their pets die.
            return !living.IsAlive ||
                   living.ObjectState != GameObject.eObjectState.Active ||
                   living.CurrentRegion != Body.CurrentRegion ||
                   !Body.IsWithinRadius(living, MAX_AGGRO_LIST_DISTANCE) ||
                   (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true) && !living.effectListComponent.ContainsEffectForEffectType(eEffect.Shade));
        }

        protected virtual bool ShouldBeIgnoredFromAggroList(GameLiving living)
        {
            // We're keeping shades in the aggro list so that mobs attack them after their pet dies, so they need to be filtered out here.
            return living.effectListComponent.ContainsEffectForEffectType(eEffect.Shade);
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
                aggroAmount.Effective = distance > EFFECTIVE_AGGRO_AMOUNT_CALCULATION_DISTANCE_THRESHOLD ?
                                        (long) Math.Ceiling(aggroAmount.Base * (EFFECTIVE_AGGRO_AMOUNT_CALCULATION_DISTANCE_THRESHOLD / distance)) :
                                        aggroAmount.Base;

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
            if (!Body.IsAlive || Body.ObjectState != GameObject.eObjectState.Active)
                return;

            if (FSM.GetCurrentState() == FSM.GetState(eFSMStateType.PASSIVE))
                return;

            ConvertDamageToAggroAmount(ad.Attacker, Math.Max(1, ad.Damage + ad.CriticalDamage));

            if (!Body.attackComponent.AttackState && FSM.GetCurrentState() != FSM.GetState(eFSMStateType.AGGRO))
            {
                FSM.SetCurrentState(eFSMStateType.AGGRO);
                Think();
            }
        }

        /// <summary>
        /// Converts a damage amount into an aggro amount, and splits it between the pet and its owner if necessary.
        /// Assumes damage to be superior than 0.
        /// </summary>
        protected virtual void ConvertDamageToAggroAmount(GameLiving attacker, int damage)
        {
            if (attacker is GameNPC NpcAttacker && NpcAttacker.Brain is ControlledMobBrain controlledBrain)
            {
                damage = controlledBrain.ModifyDamageWithTaunt(damage);

                // Aggro is split between the owner (15%) and their pet (85%).
                int aggroForOwner = (int) (damage * 0.15);

                // We must ensure that the same amount of aggro isn't added for both, otherwise an out-of-combat mob could attack the owner when their pet engages it.
                // The owner must also always generate at least 1 aggro.
                // This works as long as the split isn't 50 / 50.
                if (aggroForOwner == 0)
                {
                    AddToAggroList(controlledBrain.Owner, 1);
                    AddToAggroList(NpcAttacker, Math.Max(2, damage));
                }
                else
                {
                    AddToAggroList(controlledBrain.Owner, aggroForOwner);
                    AddToAggroList(NpcAttacker, damage - aggroForOwner);
                }
            }
            else
                AddToAggroList(attacker, damage);
        }

        #endregion

        #region Bring a Friend

        /// <summary>
        /// Max range to try to get BAFs from.
        /// May be overloaded for specific brain types, ie.dragons or keep guards
        /// </summary>
        protected virtual ushort BAFMaxRange => 2000;

        /// <summary>
        /// Max range to try to look for nearby players.
        /// May be overloaded for specific brain types, ie.dragons or keep guards
        /// </summary>
        protected virtual ushort BAFPlayerRange => 5000;

        /// <summary>
        /// Can the mob bring a friend?
        /// Set to false when a mob BAFs or is brought by a friend.
        /// </summary>
        public virtual bool CanBAF { get; set; } = true;

        /// <summary>
        /// Bring friends when this mob aggros
        /// </summary>
        protected virtual void BringFriends(GameLiving puller)
        {
            if (!CanBAF || Body.Faction == null)
                return;

            GamePlayer playerPuller;

            // Only BAF on players and pets of players
            if (puller is GamePlayer)
                playerPuller = (GamePlayer) puller;
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
            CanBAF = false; // Mobs only BAF once per fight
            int maxAdds = GetMaxAddsCountFromBaf(puller, out List<GamePlayer> otherTargets);
            IEnumerable<StandardMobBrain> brainsInRadius = GetFriendlyAndAvailableBrainsInRadiusOrderedByDistance(BAFMaxRange, maxAdds);

            foreach (StandardMobBrain brain in brainsInRadius)
            {
                brain.CanBAF = false;
                GameLiving target;

                if (otherTargets != null && otherTargets.Count > 1)
                    target = otherTargets[Util.Random(0, otherTargets.Count - 1)];
                else
                    target = puller;

                brain.AddToAggroList(target, 1);
                brain.AttackMostWanted();
            }

            int GetMaxAddsCountFromBaf(GameLiving puller, out List<GamePlayer> otherTargets)
            {
                int numAttackers = 0;
                otherTargets = null;
                HashSet<string> countedVictims = null;
                HashSet<string> countedAttackers = null;
                BattleGroup bg = puller.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                if (puller.Group is Group group)
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
                        if (playerInGroup != null && (playerInGroup.InternalID == puller.InternalID || playerInGroup.IsWithinRadius(puller, BAFPlayerRange, true)))
                        {
                            numAttackers++;
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
                        if (player2 != null && (player2.InternalID == puller.InternalID || player2.IsWithinRadius(puller, BAFPlayerRange, true)))
                        {
                            if (Properties.BAF_MOBS_COUNT_BG_MEMBERS && (countedAttackers == null || !countedAttackers.Contains(player2.InternalID)))
                                numAttackers++;

                            if (otherTargets != null && (countedVictims == null || !countedVictims.Contains(player2.InternalID)))
                                otherTargets.Add(player2);
                        }
                    }
                }

                // Player is alone.
                if (numAttackers == 0)
                    numAttackers = 1;

                int percentBAF = Properties.BAF_INITIAL_CHANCE + (numAttackers - 1) * Properties.BAF_ADDITIONAL_CHANCE;
                int maxAdds = percentBAF / 100; // Multiple of 100 are guaranteed BAFs.

                // Calculate chance of an addition add based on the remainder.
                if (Util.Chance(percentBAF % 100))
                    maxAdds++;

                return Math.Max(0, maxAdds);
            }
        }

        public IEnumerable<StandardMobBrain> GetFriendlyAndAvailableBrainsInRadiusOrderedByDistance(ushort radius, int count)
        {
            return Body.GetNPCsInRadius(radius).Where(WherePredicate).OrderBy(OrderByPredicate).Take(count).Select(SelectPredicate);

            bool WherePredicate(GameNPC npc)
            {
                return npc != Body && npc.IsFriend(Body) && npc.IsAggressive && npc.CanJoinFight;
            }

            long OrderByPredicate(GameNPC npc)
            {
                int xDiff = Body.X - npc.X;
                int yDiff = Body.Y - npc.Y;
                int zDiff = Body.Z - npc.Z;
                return xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;
            }

            static StandardMobBrain SelectPredicate(GameNPC npc)
            {
                return npc.Brain as StandardMobBrain;
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

            bool casted = false;

            if (type is eCheckSpellType.Defensive)
            {
                if (Body.CanCastInstantHealSpells)
                    CheckDefensiveSpells(Body.InstantHealSpells);

                if (Body.CanCastInstantMiscSpells)
                    CheckDefensiveSpells(Body.InstantMiscSpells);

                if (Body.CanCastHealSpells)
                    casted = CheckDefensiveSpells(Body.HealSpells);

                if (!casted && Body.CanCastMiscSpells)
                    casted = CheckDefensiveSpells(Body.MiscSpells);
            }
            else if (type is eCheckSpellType.Offensive)
            {
                if (Body.CanCastInstantHarmfulSpells)
                    CheckOffensiveSpells(Body.InstantHarmfulSpells);

                if (Body.CanCastHarmfulSpells)
                    casted = CheckOffensiveSpells(Body.HarmfulSpells);
            }

            return casted || Body.IsCasting;

            bool CheckOffensiveSpells(List<Spell> spells)
            {
                List<Spell> spellsToCast = new(spells.Count);

                foreach (Spell spell in spells)
                {
                    if (CanCastOffensiveSpell(spell))
                        spellsToCast.Add(spell);
                }

                return spellsToCast.Count > 0 && Body.CastSpell(spellsToCast[Util.Random(spellsToCast.Count - 1)], m_mobSpellLine);

                bool CanCastOffensiveSpell(Spell spell)
                {
                    if ((!spell.Uninterruptible && Body.IsBeingInterrupted) ||
                        (spell.HasRecastDelay && Body.GetSkillDisabledDuration(spell) > 0))
                    {
                        return false;
                    }

                    return Body.TargetObject is GameLiving target &&
                           Body.IsWithinRadius(target, spell.Range) &&
                           ((spell.Duration <= 0 && !spell.IsConcentration) || !LivingHasEffect(target, spell) || spell.SpellType is eSpellType.DirectDamageWithDebuff or eSpellType.DamageSpeedDecrease);
                }
            }

            bool CheckDefensiveSpells(List<Spell> spells)
            {
                // Contrary to offensive spells, we don't start with a valid target.
                // So the idea here is to find a target, switch before calling `CastDefensiveSpell`, then retrieve our previous target.
                List<(Spell, GameLiving)> spellsToCast = new(spells.Count);

                foreach (Spell spell in spells)
                {
                    if (CanCastDefensiveSpell(spell, out GameLiving target))
                        spellsToCast.Add((spell, target));
                }

                if (spellsToCast.Count == 0)
                    return false;

                GameObject oldTarget = Body.TargetObject;
                (Spell spell, GameLiving target) spellToCast = spellsToCast[Util.Random(spellsToCast.Count - 1)];
                Body.TargetObject = spellToCast.target;
                bool cast = Body.CastSpell(spellToCast.spell, m_mobSpellLine);
                Body.TargetObject = oldTarget;
                return cast;

                bool CanCastDefensiveSpell(Spell spell, out GameLiving target)
                {
                    target = null;

                    if ((!spell.Uninterruptible && Body.IsBeingInterrupted) ||
                        (spell.HasRecastDelay && Body.GetSkillDisabledDuration(spell) > 0))
                    {
                        return false;
                    }

                    target = FindTargetForDefensiveSpell(spell);
                    return target != null;
                }
            }
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
                case eSpellType.ArmorAbsorptionBuff:
                case eSpellType.ArmorFactorBuff:
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
                case eSpellType.PaladinArmorFactorBuff:
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
                    if (!LivingHasEffect(Body, spell) && !Body.attackComponent.AttackState && spell.Target != eSpellTarget.PET)
                    {
                        target = Body;
                        break;
                    }

                    if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && !LivingHasEffect(Body.ControlledBrain.Body, spell) && spell.Target != eSpellTarget.SELF)
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

                    if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && Body.ControlledBrain.Body.IsDiseased
                        && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && spell.Target != eSpellTarget.SELF)
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

                    if (Body.ControlledBrain != null &&
                        Body.ControlledBrain.Body != null &&
                        Body.ControlledBrain.Body.IsPoisoned &&
                        Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && spell.Target != eSpellTarget.SELF)
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

                    if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null
                        && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range
                        && Body.ControlledBrain.Body.HealthPercent < Properties.NPC_HEAL_THRESHOLD
                        && spell.Target != eSpellTarget.SELF)
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

            eEffect spellEffect = EffectService.GetEffectFromSpell(spell, m_mobSpellLine.IsBaseLine);

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

            ISpellHandler spellHandler = Body.castingComponent.SpellHandler;

            // If we're currently casting 'spell' on 'target', assume it already has the effect.
            // This allows spell queuing while preventing casting on the same target more than once.
            if (spellHandler != null && spellHandler.Spell.ID == spell.ID && spellHandler.Target == target)
                return true;

            ISpellHandler queuedSpellHandler = Body.castingComponent.QueuedSpellHandler;

            // Do the same for our queued up spell.
            // This can happen on charmed pets having two buffs that they're trying to cast on their owner.
            if (queuedSpellHandler != null && queuedSpellHandler.Spell.ID == spell.ID && queuedSpellHandler.Target == target)
                return true;

            // May not be the right place for that, but without that check NPCs with more than one offensive or defensive proc will only buff themselves once.
            if (spell.SpellType is eSpellType.OffensiveProc or eSpellType.DefensiveProc)
            {
                if (target.effectListComponent.Effects.TryGetValue(EffectService.GetEffectFromSpell(spell, m_mobSpellLine.IsBaseLine), out List<ECSGameEffect> existingEffects))
                {
                    if (existingEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == spell.ID || (spell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == spell.EffectGroup)) != null)
                        return true;
                }

                return false;
            }

            // True if the target has the effect, or the immunity effect for this effect.
            // Treat NPC immunity effects as full immunity effects.
            return EffectListService.GetEffectOnTarget(target, spellEffect) != null || HasImmunityEffect(EffectService.GetImmunityEffectFromSpell(spell)) || HasImmunityEffect(EffectService.GetNpcImmunityEffectFromSpell(spell));

            bool HasImmunityEffect(eEffect immunityEffect)
            {
                return immunityEffect != eEffect.Unknown && EffectListService.GetEffectOnTarget(target, immunityEffect) != null;
            }
        }

        #endregion

        #region DetectDoor

        public virtual void DetectDoor()
        {
            ushort range = (ushort) (ThinkInterval / 800 * Body.CurrentWaypoint.MaxSpeed);

            foreach (GameDoorBase door in Body.CurrentRegion.GetDoorsInRadius(Body, range))
            {
                if (door is GameKeepDoor)
                {
                    if (Body.Realm != door.Realm)
                        return;

                    door.Open();
                    //Body.Say("GameKeep Door is near by");
                    //somebody can insert here another action for GameKeep Doors
                    return;
                }
                else
                {
                    door.Open();
                    return;
                }
            }

            return;
        }
        #endregion
    }
}
