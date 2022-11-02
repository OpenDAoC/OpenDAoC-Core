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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
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

        // Used for AmbientBehaviour "Seeing" - maintains a list of GamePlayer in range
        public List<GamePlayer> PlayersSeen = new List<GamePlayer>();

        public new StandardMobFSM FSM { get; private set; }

        /// <summary>
        /// Constructs a new StandardMobBrain
        /// </summary>
        public StandardMobBrain() : base()
        {
            AggroLevel = 0;
            AggroRange = 0;

            FSM = new StandardMobFSM();
            FSM.Add(new StandardMobState_IDLE(FSM, this));
            FSM.Add(new StandardMobState_WAKING_UP(FSM, this));
            FSM.Add(new StandardMobState_AGGRO(FSM, this));
            FSM.Add(new StandardMobState_RETURN_TO_SPAWN(FSM, this));
            FSM.Add(new StandardMobState_PATROLLING(FSM, this));
            FSM.Add(new StandardMobState_ROAMING(FSM, this));
            FSM.Add(new StandardMobState_DEAD(FSM, this));

            FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        }

        /// <summary>
        /// Returns the string representation of the StandardMobBrain
        /// </summary>
        /// <returns></returns>
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

        public bool ShouldCheckProximityAggro = true;

        public virtual bool CheckProximityAggro()
        {
            FireAmbientSentence();

            // Check aggro only if we're not in combat.
            if (ShouldCheckProximityAggro && !HasAggro && !Body.AttackState && Body.CurrentSpellHandler == null) 
            {
                // Don't check aggro if we spawned less than X seconds ago. This is to prevent clients from sending positive LoS check
                // when they shouldn't, which can happen right after SendNPCCreate and makes mobs aggro through walls.
                if (GameLoop.GameLoopTime - Body.SpawnTick < 1250)
                    return false;

                CheckPlayerAggro();
                CheckNPCAggro();
            }

            // Some calls rely on this method to return if there's something in the aggro list, not necesarilly to perform a proximity aggro check.
            // But this doesn't necessarily return wheter or not the check was positive, only the current state (LoS checks take time).
            return HasAggro;
        }

        public virtual bool IsBeyondTetherRange()
        {
            if (Body.MaxDistance != 0)
            {
                int distance = Body.GetDistanceTo(Body.SpawnPoint);
                int maxdistance = Body.MaxDistance > 0 ? Body.MaxDistance : -Body.MaxDistance * AggroRange / 100;

                if (maxdistance > 0 && distance > maxdistance)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public virtual bool HasPatrolPath()
        {
            if (Body.MaxSpeedBase > 0
                && Body.CurrentSpellHandler == null
                && !Body.IsMoving
                && !Body.attackComponent.AttackState
                && !Body.InCombat
                && !Body.IsMovingOnPath
                && Body.PathID != null
                && Body.PathID != ""
                && Body.PathID != "NULL")
                return true;
            else
                return false;
        }

        public long LastNPCAggroCheckTick = 0;

        /// <summary>
        /// Check for aggro against players
        /// </summary>
        protected virtual void CheckPlayerAggro()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange, !Body.CurrentZone.IsDungeon))
            {
                if (!CanAggroTarget(player))
                    continue;
                if (player.IsStealthed || player.Steed != null)
                    continue;
                if (player.EffectList.GetOfType<NecromancerShadeEffect>() != null)
                    continue;

                if (GS.ServerProperties.Properties.ALWAYS_CHECK_LOS)
                    // We don't know if the LoS check will be positive, so we have to ask other players
                    player.Out.SendCheckLOS(Body, player, new CheckLOSResponse(LosCheckForAggroCallback));
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
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange, !Body.CurrentRegion.IsDungeon))
            {
                if (!CanAggroTarget(npc))
                    continue;
                if (npc is GameTaxi or GameTrainingDummy)
                    continue;

                if (GS.ServerProperties.Properties.ALWAYS_CHECK_LOS)
                {
                    // Check LoS if either the target or the current mob is a pet
                    if (npc.Brain is ControlledNpcBrain theirControlledNpcBrain && theirControlledNpcBrain.Owner is GamePlayer theirOwner)
                    {
                        theirOwner.Out.SendCheckLOS(Body, npc, new CheckLOSResponse(LosCheckForAggroCallback));
                        continue;
                    }
                    else if (this is ControlledNpcBrain ourControlledNpcBrain && ourControlledNpcBrain.Owner is GamePlayer ourOwner)
                    {
                        ourOwner.Out.SendCheckLOS(Body, npc, new CheckLOSResponse(LosCheckForAggroCallback));
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
                var currentPlayersSeen = new List<GamePlayer>();
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange, true))
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
        public virtual int AggroRange { get => Math.Min(_aggroRange, MAX_AGGRO_DISTANCE); set => _aggroRange = value; }

        /// <summary>
        /// Aggressive Level in % 0..100, 0 means not Aggressive
        /// </summary>
        public virtual int AggroLevel { get; set; }

        /// <summary>
        /// List of livings that this npc has aggro on, living => aggroamount
        /// </summary>
        public Dictionary<GameLiving, long> AggroTable { get; private set; } = new Dictionary<GameLiving, long>();

        /// <summary>
        /// Checks whether living has someone on its aggrolist
        /// </summary>
        public virtual bool HasAggro
        {
            get
            {
                bool hasAggro = false;

                lock ((AggroTable as ICollection).SyncRoot)
                {
                    hasAggro = AggroTable.Count > 0;
                }

                return hasAggro;
            }
        }

        /// <summary>
        /// Add aggro table of this brain to that of another living.
        /// </summary>
        /// <param name="brain">The target brain.</param>
        public void AddAggroListTo(StandardMobBrain brain)
        {
            if (!brain.Body.IsAlive)
                return;

            KeyValuePair<GameLiving, long>[] aggroTable = Array.Empty<KeyValuePair<GameLiving, long>>();

            lock ((AggroTable as ICollection).SyncRoot)
                aggroTable = AggroTable.ToArray();

            foreach (var aggro in aggroTable)
                brain.AddToAggroList(aggro.Key, Body.MaxHealth);
        }

        /// <summary>
        /// Add living to the aggrolist
        /// aggroamount can be negative to lower amount of aggro
        /// </summary>
        /// <param name="living"></param>
        /// <param name="aggroamount"></param>
        /// <param name="CheckLOS"></param>
        public virtual void AddToAggroList(GameLiving living, int aggroamount)
        {
            // tolakram - duration spell effects will attempt to add to aggro after npc is dead
            if (Body.IsConfused || !Body.IsAlive || living == null)
                return;

            BringFriends(living);

            // Handle trigger to say sentance on first aggro.
            if (AggroTable.Count < 1)
                Body.FireAmbientSentence(GameNPC.eAmbientTrigger.aggroing, living);

            // Only protect if gameplayer and aggroamout > 0
            if (living is GamePlayer player && aggroamount > 0)
            {
                // If player is in group, add whole group to aggro list
                if (player.Group != null)
                { 
                    lock ((AggroTable as ICollection).SyncRoot)
                    {
                        foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
                        {
                            if (!AggroTable.ContainsKey(p))
                                AggroTable[p] = 1L; // Add the missing group member on aggro table
                        }
                    }
                }

                foreach (ProtectECSGameEffect protect in player.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == eEffect.Protect))
                {
                    if (aggroamount <= 0)
                        break;

                    if (protect.ProtectTarget != living)
                        continue;

                    GamePlayer protectSource = protect.ProtectSource;

                    if (protectSource.IsStunned
                        || protectSource.IsMezzed
                        || protectSource.IsSitting
                        || protectSource.ObjectState != GameObject.eObjectState.Active
                        || !protectSource.IsAlive
                        || !protectSource.InCombat)
                        continue;

                    if (!living.IsWithinRadius(protectSource, ProtectAbilityHandler.PROTECT_DISTANCE))
                        continue;

                    // P I: prevents 10% of aggro amount
                    // P II: prevents 20% of aggro amount
                    // P III: prevents 30% of aggro amount
                    // guessed percentages, should never be higher than or equal to 50%
                    int abilityLevel = protectSource.GetAbilityLevel(Abilities.Protect);
                    int protectAmount = (int)(abilityLevel * 0.10 * aggroamount);

                    if (protectAmount > 0)
                    {
                        aggroamount -= protectAmount;
                        protectSource.Out.SendMessage(LanguageMgr.GetTranslation(protectSource.Client.Account.Language, "AI.Brain.StandardMobBrain.YouProtDist", player.GetName(0, false),
                                                                                 Body.GetName(0, false, protectSource.Client.Account.Language, Body)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        lock ((AggroTable as ICollection).SyncRoot)
                        {
                            if (AggroTable.ContainsKey(protectSource))
                                AggroTable[protectSource] += protectAmount;
                            else
                                AggroTable[protectSource] = protectAmount;
                        }
                    }
                }
            }

            lock ((AggroTable as ICollection).SyncRoot)
            {
                if (AggroTable.ContainsKey(living))
                {
                    long amount = AggroTable[living];
                    amount += aggroamount;

                    // can't be removed this way, set to minimum
                    if (amount <= 0)
                        amount = 1L;

                    AggroTable[living] = amount;
                }
                else
                {
                    if (aggroamount > 0)
                        AggroTable[living] = aggroamount;
                    else
                        AggroTable[living] = 1L;
                }
            }
        }

        public void PrintAggroTable()
        {
            StringBuilder sb = new StringBuilder();
            foreach (GameLiving gl in AggroTable.Keys)
                sb.AppendLine("Living: " + gl.Name + ", aggro: " + AggroTable[gl].ToString());
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Get current amount of aggro on aggrotable
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public virtual long GetAggroAmountForLiving(GameLiving living)
        {
            lock ((AggroTable as ICollection).SyncRoot)
            {
                if (AggroTable.ContainsKey(living))
                    return AggroTable[living];
                return 0;
            }
        }

        /// <summary>
        /// Remove one living from aggro list
        /// </summary>
        /// <param name="living"></param>
        public virtual void RemoveFromAggroList(GameLiving living)
        {
            lock ((AggroTable as ICollection).SyncRoot)
                AggroTable.Remove(living);
        }

        /// <summary>
        /// Remove all livings from the aggrolist
        /// </summary>
        public virtual void ClearAggroList()
        {
            CanBAF = true; // Mobs that drop out of combat can BAF again

            lock ((AggroTable as ICollection).SyncRoot)
            {
                AggroTable.Clear();
                Body.TempProperties.removeProperty(Body.attackComponent.Attackers);
            }
        }

        /// <summary>
        /// Makes a copy of current aggro list
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<GameLiving, long> CloneAggroList()
        {
            lock ((AggroTable as ICollection).SyncRoot)
                return new Dictionary<GameLiving, long>(AggroTable);
        }

        /// <summary>
        /// Selects and attacks the next target or does nothing
        /// </summary>
        public virtual void AttackMostWanted()
        {
            if (!IsActive)
                return;

            if (ECS.Debug.Diagnostics.AggroDebugEnabled)
                PrintAggroTable();

            Body.TargetObject = CalculateNextAttackTarget();

            if (Body.TargetObject != null)
            {
                if (!CheckSpells(eCheckSpellType.Offensive))
                    Body.StartAttack(Body.TargetObject);
            }
        }

        /// <summary>
        /// Callback for when we receive a LoS check reply
        /// </summary>
        protected virtual void LosCheckForAggroCallback(GamePlayer player, ushort response, ushort targetOID)
        {
            // If we kept adding to the aggro list it would make mobs go from one target immediately to another
            // For whatever reason, a call to CheckLOSResponse will result in this method being called with 0 arguments. We need to filter that out
            if (HasAggro || targetOID == 0)
                return; 

            if ((response & 0x100) == 0x100)
            {
                GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

                if (gameObject is GameLiving gameLiving)
                    AddToAggroList(gameLiving, 1);
            }
        }

        protected virtual void LosCheckInCombatCallback(GamePlayer player, ushort response, ushort targetOID)
        {
            // For whatever reason, a call to CheckLOSResponse will result in this method being called with 0 arguments. We need to filter that out
            if (targetOID == 0)
                return;

            if ((response & 0x100) != 0x100)
            {
                GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

				if (gameObject is GameLiving gameLiving)
                {
                    FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                    RemoveFromAggroList(gameLiving);
                }
            }
        }

        /// <summary>
        /// Returns the best target to attack
        /// </summary>
        /// <returns>the best target</returns>
        protected virtual GameLiving CalculateNextAttackTarget()
        {
            GameLiving maxAggroObject = null;
            List<KeyValuePair<GameLiving, long>> aggroList = new List<KeyValuePair<GameLiving, long>>();

            lock ((AggroTable as ICollection).SyncRoot)
                aggroList = AggroTable.ToList();

            double maxAggro = 0;
            List<GameLiving> removable = new List<GameLiving>();
            
            foreach (var currentKey in aggroList)
            {
                GameLiving living = currentKey.Key;

                if (living == null)
                    continue;

                // Check to make sure this target is still valid.
                if (!living.IsAlive
                    || living.ObjectState != GameObject.eObjectState.Active
                    || living.IsStealthed
                    || Body.GetDistanceTo(living, 0) > MAX_AGGRO_LIST_DISTANCE
                    || !GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
                {
                    // Keep Necromancer shades so that we can attack them if their pets die.
                    if (living.EffectList.GetOfType<NecromancerShadeEffect>() != null)
                        removable.Add(living);
                    continue;
                }

                long amount = currentKey.Value;

                if (living.IsAlive
                    && amount > maxAggro
                    && living.CurrentRegion == Body.CurrentRegion
                    && living.ObjectState == GameObject.eObjectState.Active)
                {
                    double aggro = amount * Math.Min(500.0 / Body.GetDistanceTo(living), 1);
                    if (aggro > maxAggro)
                    {
                        maxAggroObject = living;
                        maxAggro = aggro;
                    }
                }
            }

            foreach (GameLiving l in removable)
            {
                RemoveFromAggroList(l);
                Body.attackComponent.RemoveAttacker(l);
            }

            if (maxAggroObject == null)
            {
                lock ((AggroTable as ICollection).SyncRoot)
                    AggroTable.Clear();
            }

            return maxAggroObject;
        }

        public virtual bool CanAggroTarget(GameLiving target)
        {
            if (!GameServer.ServerRules.IsAllowedToAttack(Body, target, true))
                return false;

            // Get owner if target is pet or subpet
            GameLiving realTarget = target;
            if (target is GamePet pet && (pet.Owner is GamePlayer || pet.Owner is GamePet))
            {
                if (pet.Owner is GamePet mainpet && mainpet.Owner is GamePlayer)
                    realTarget = mainpet.Owner;
                else
                    realTarget = pet.Owner;
            }

            // Only attack if green+ to target
            if (realTarget.IsObjectGreyCon(Body))
                return false;

            // If this npc have Faction return the AggroAmount to Player
            if (Body.Faction != null)
            {
                if (realTarget is GamePlayer)
                    return Body.Faction.GetAggroToFaction((GamePlayer)realTarget) > 75;
                else if (realTarget is GameNPC && Body.Faction.EnemyFactions.Contains(((GameNPC)realTarget).Faction))
                    return true;
            }

            // We put this here to prevent aggroing non-factions npcs
            if (Body.Realm == eRealm.None && realTarget is GameNPC)
                return false;

            return AggroLevel > 0;
        }

        /// <summary>
        /// Receives all messages of the body
        /// </summary>
        /// <param name="e">The event received</param>
        /// <param name="sender">The event sender</param>
        /// <param name="args">The event arguments</param>
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
        }
        
        /// <summary>
        /// Lost follow target event
        /// </summary>
        /// <param name="target"></param>
        protected virtual void OnFollowLostTarget(GameObject target)
        {
            AttackMostWanted();
            if (!Body.attackComponent.AttackState)
                Body.WalkToSpawn();
        }

        /// <summary>
        /// Attacked by enemy event
        /// </summary>
        /// <param name="ad"></param>
        public virtual void OnAttackedByEnemy(AttackData ad)
        {
            if (FSM.GetCurrentState() == FSM.GetState(eFSMStateType.PASSIVE))
                return;
    
            if (!Body.attackComponent.AttackState
                && Body.IsAlive
                && Body.ObjectState == GameObject.eObjectState.Active)
            {
                if (ad.AttackResult == eAttackResult.Missed)
                    AddToAggroList(ad.Attacker, 1);
                else
                    AddToAggroList(ad.Attacker, ad.Damage + ad.CriticalDamage);

                if (FSM.GetCurrentState() != FSM.GetState(eFSMStateType.AGGRO))
                {
                    if (this is CommanderBrain cBrain)
                        cBrain.Attack(ad.Attacker);
                    FSM.SetCurrentState(eFSMStateType.AGGRO);
                    FSM.Think();
                }
            }
        }

        #endregion

        #region Bring a Friend

        /// <summary>
        /// Initial range to try to get BAFs from.
        /// May be overloaded for specific brain types, ie. dragons or keep guards
        /// </summary>
        protected virtual ushort BAFInitialRange {
            get { return 250; }
        }

        /// <summary>
        /// Max range to try to get BAFs from.
        /// May be overloaded for specific brain types, ie.dragons or keep guards
        /// </summary>
        protected virtual ushort BAFMaxRange {
            get { return 2000; }
        }

        /// <summary>
        /// Max range to try to look for nearby players.
        /// May be overloaded for specific brain types, ie.dragons or keep guards
        /// </summary>
        protected virtual ushort BAFPlayerRange {
            get { return 5000; }
        }

        /// <summary>
        /// Can the mob bring a friend?
        /// Set to false when a mob BAFs or is brought by a friend.
        /// </summary>
        public virtual bool CanBAF { get; set; } = true;

        /// <summary>
        /// Bring friends when this mob aggros
        /// </summary>
        /// <param name="attacker">Whoever triggered the BAF</param>
        protected virtual void BringFriends(GameLiving attacker)
        {
            if (!CanBAF)
                return;

            GamePlayer puller;  // player that triggered the BAF
            GameLiving actualPuller;

            // Only BAF on players and pets of players
            if (attacker is GamePlayer)
            {
                puller = (GamePlayer)attacker;
                actualPuller = puller;
            }
            else if (attacker is GamePet pet && pet.Owner is GamePlayer owner)
            {
                puller = owner;
                actualPuller = attacker;
            }
            else if (attacker is BDSubPet bdSubPet && bdSubPet.Owner is GamePet bdPet && bdPet.Owner is GamePlayer bdOwner)
            {
                puller = bdOwner;
                actualPuller = bdPet;
            }
            else
                return;

            CanBAF = false; // Mobs only BAF once per fight

            int numAttackers = 0;

            List<GamePlayer> victims = null; // Only instantiated if we're tracking potential victims

            // These are only used if we have to check for duplicates
            HashSet<String> countedVictims = null;
            HashSet<String> countedAttackers = null;

            BattleGroup bg = puller.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null) as BattleGroup;

            // Check group first to minimize the number of HashSet.Add() calls
            if (puller.Group is Group group)
            {
                if (DOL.GS.ServerProperties.Properties.BAF_MOBS_COUNT_BG_MEMBERS && bg != null)
                    countedAttackers = new HashSet<String>(); // We have to check for duplicates when counting attackers

                if (!DOL.GS.ServerProperties.Properties.BAF_MOBS_ATTACK_PULLER)
                {
                    if (DOL.GS.ServerProperties.Properties.BAF_MOBS_ATTACK_BG_MEMBERS && bg != null)
                    {
                        // We need a large enough victims list for group and BG, and also need to check for duplicate victims
                        victims = new List<GamePlayer>(group.MemberCount + bg.PlayerCount - 1);
                        countedVictims = new HashSet<String>();
                    }
                    else
                        victims = new List<GamePlayer>(group.MemberCount);
                }

                foreach (GamePlayer player in group.GetPlayersInTheGroup())
                    if (player != null && (player.InternalID == puller.InternalID || player.IsWithinRadius(puller, BAFPlayerRange, true)))
                    {
                        numAttackers++;

                        if (countedAttackers != null)
                            countedAttackers.Add(player.InternalID);

                        if (victims != null)
                        {
                            victims.Add(player);

                            if (countedVictims != null)
                                countedVictims.Add(player.InternalID);
                        }
                    }
            } // if (puller.Group is Group group)

            // Do we have to count BG members, or add them to victims list?
            if ((bg != null) && (DOL.GS.ServerProperties.Properties.BAF_MOBS_COUNT_BG_MEMBERS
                || (DOL.GS.ServerProperties.Properties.BAF_MOBS_ATTACK_BG_MEMBERS && !DOL.GS.ServerProperties.Properties.BAF_MOBS_ATTACK_PULLER)))
            {
                if (victims == null && DOL.GS.ServerProperties.Properties.BAF_MOBS_ATTACK_BG_MEMBERS && !DOL.GS.ServerProperties.Properties.BAF_MOBS_ATTACK_PULLER)
                    // Puller isn't in a group, so we have to create the victims list for the BG
                    victims = new List<GamePlayer>(bg.PlayerCount);

                foreach (GamePlayer player in bg.Members.Keys)
                    if (player != null && (player.InternalID == puller.InternalID || player.IsWithinRadius(puller, BAFPlayerRange, true)))
                    {
                        if (DOL.GS.ServerProperties.Properties.BAF_MOBS_COUNT_BG_MEMBERS
                            && (countedAttackers == null || !countedAttackers.Contains(player.InternalID)))
                            numAttackers++;

                        if (victims != null && (countedVictims == null || !countedVictims.Contains(player.InternalID)))
                            victims.Add(player);
                    }
            } // if ((bg != null) ...

            if (numAttackers == 0)
                // Player is alone
                numAttackers = 1;

            int percentBAF = DOL.GS.ServerProperties.Properties.BAF_INITIAL_CHANCE
                + ((numAttackers - 1) * DOL.GS.ServerProperties.Properties.BAF_ADDITIONAL_CHANCE);

            int maxAdds = percentBAF / 100; // Multiple of 100 are guaranteed BAFs

            // Calculate chance of an addition add based on the remainder
            if (Util.Chance(percentBAF % 100))
                maxAdds++;

            if (maxAdds > 0)
            {
                int numAdds = 0; // Number of mobs currently BAFed
                ushort range = BAFInitialRange; // How far away to look for friends

                // Try to bring closer friends before distant ones.
                while (numAdds < maxAdds && range <= BAFMaxRange)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(range))
                    {
                        if (numAdds >= maxAdds)
                            break;

                        // If it's a friend, have it attack
                        if (npc.IsFriend(Body) && npc.IsAggressive && npc.IsAvailable && npc.Brain is StandardMobBrain brain)
                        {
                            brain.CanBAF = false; // Mobs brought cannot bring friends of their own

                            GameLiving target;
                            if (victims != null && victims.Count > 0)
                                target = victims[Util.Random(0, victims.Count - 1)];
                            else
                                target = actualPuller;

                            brain.AddToAggroList(target, 1);
                            brain.AttackMostWanted();
                            numAdds++;
                        }
                    }// foreach

                    // Increase the range for finding friends to join the fight.
                    range *= 2;
                } // while
            } // if (maxAdds > 0)
        } // BringFriends()

        #endregion

        #region Spells

        public enum eCheckSpellType
        {
            Offensive,
            Defensive
        }

        /// <summary>
        /// Checks if any spells need casting
        /// </summary>
        /// <param name="type">Which type should we go through and check for?</param>
        /// <returns></returns>
        public virtual bool CheckSpells(eCheckSpellType type)
        {
            if (Body.IsCasting)
                return true;

            bool casted = false;

            if (Body != null && Body.Spells != null && Body.Spells.Count > 0)
            {
                ArrayList spell_rec = new ArrayList();
                Spell spellToCast = null;
                bool needpet = false;
                bool needheal = false;

                if (type == eCheckSpellType.Defensive)
                {
                    foreach (Spell spell in Body.Spells)
                    {
                        if (Body.GetSkillDisabledDuration(spell) > 0) continue;
                        if (spell.Target.ToLower() == "enemy" || spell.Target.ToLower() == "area" || spell.Target.ToLower() == "cone") continue;
                        // If we have no pets
                        if (Body.ControlledBrain == null)
                        {
                            if (spell.SpellType == (byte)eSpellType.Pet) continue;

                            // TODO: Need to fix this bit
                            //if (spell.SpellType.ToLower().Contains("summon"))
                            //{
                            //	spell_rec.Add(spell);
                            //	needpet = true;
                            //}
                        }
                        if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null)
                        {
                            if (Util.Chance(30) && Body.ControlledBrain != null && spell.SpellType == (byte)eSpellType.Heal &&
                                Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range &&
                                Body.ControlledBrain.Body.HealthPercent < DOL.GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD
                                && spell.Target.ToLower() != "self")
                            {
                                spell_rec.Add(spell);
                                needheal = true;
                            }
                            if (LivingHasEffect(Body.ControlledBrain.Body, spell) && (spell.Target.ToLower() != "self")) continue;
                        }
                        if (!needpet && !needheal)
                            spell_rec.Add(spell);
                    }
                    if (spell_rec.Count > 0)
                    {
                        spellToCast = (Spell)spell_rec[Util.Random((spell_rec.Count - 1))];
                        if (!Body.IsReturningToSpawnPoint)
                        {
                            if (spellToCast.Uninterruptible && CheckDefensiveSpells(spellToCast))
                                casted = true;
                            else
                                if (!Body.IsBeingInterrupted && CheckDefensiveSpells(spellToCast))
                                casted = true;
                        }
                    }
                }
                else if (type == eCheckSpellType.Offensive)
                {
                    foreach (Spell spell in Body.Spells)
                    {

                        if (Body.GetSkillDisabledDuration(spell) == 0)
                        {
                            if (spell.CastTime > 0)
                            {
                                if (spell.Target.ToLower() == "enemy" || spell.Target.ToLower() == "area" || spell.Target.ToLower() == "cone")
                                    spell_rec.Add(spell);
                            }
                        }
                    }
                    if (spell_rec.Count > 0)
                    {
                        spellToCast = (Spell)spell_rec[Util.Random((spell_rec.Count - 1))];


                        if (spellToCast.Uninterruptible && CheckOffensiveSpells(spellToCast))
                            casted = true;
                        else
                            if (!Body.IsBeingInterrupted && CheckOffensiveSpells(spellToCast))
                            casted = true;
                    }
                }

                return casted;
            }
            return casted;
        }

        /// <summary>
        /// Checks defensive spells.  Handles buffs, heals, etc.
        /// </summary>
        protected virtual bool CheckDefensiveSpells(Spell spell)
        {
            if (spell == null) return false;
            if (Body.GetSkillDisabledDuration(spell) > 0) return false;

            bool casted = false;

            // clear current target, set target based on spell type, cast spell, return target to original target
            GameObject lastTarget = Body.TargetObject;

            Body.TargetObject = null;
            switch (spell.SpellType)
            {
                #region Buffs
                case (byte)eSpellType.AcuityBuff:
                case (byte)eSpellType.AFHitsBuff:
                case (byte)eSpellType.AllMagicResistBuff:
                case (byte)eSpellType.ArmorAbsorptionBuff:
                case (byte)eSpellType.ArmorFactorBuff:
                case (byte)eSpellType.BodyResistBuff:
                case (byte)eSpellType.BodySpiritEnergyBuff:
                case (byte)eSpellType.Buff:
                case (byte)eSpellType.CelerityBuff:
                case (byte)eSpellType.ColdResistBuff:
                case (byte)eSpellType.CombatSpeedBuff:
                case (byte)eSpellType.ConstitutionBuff:
                case (byte)eSpellType.CourageBuff:
                case (byte)eSpellType.CrushSlashTrustBuff:
                case (byte)eSpellType.DexterityBuff:
                case (byte)eSpellType.DexterityQuicknessBuff:
                case (byte)eSpellType.EffectivenessBuff:
                case (byte)eSpellType.EnduranceRegenBuff:
                case (byte)eSpellType.EnergyResistBuff:
                case (byte)eSpellType.FatigueConsumptionBuff:
                case (byte)eSpellType.FlexibleSkillBuff:
                case (byte)eSpellType.HasteBuff:
                case (byte)eSpellType.HealthRegenBuff:
                case (byte)eSpellType.HeatColdMatterBuff:
                case (byte)eSpellType.HeatResistBuff:
                case (byte)eSpellType.HeroismBuff:
                case (byte)eSpellType.KeepDamageBuff:
                case (byte)eSpellType.MagicResistBuff:
                case (byte)eSpellType.MatterResistBuff:
                case (byte)eSpellType.MeleeDamageBuff:
                case (byte)eSpellType.MesmerizeDurationBuff:
                case (byte)eSpellType.MLABSBuff:
                case (byte)eSpellType.PaladinArmorFactorBuff:
                case (byte)eSpellType.ParryBuff:
                case (byte)eSpellType.PowerHealthEnduranceRegenBuff:
                case (byte)eSpellType.PowerRegenBuff:
                case (byte)eSpellType.SavageCombatSpeedBuff:
                case (byte)eSpellType.SavageCrushResistanceBuff:
                case (byte)eSpellType.SavageDPSBuff:
                case (byte)eSpellType.SavageParryBuff:
                case (byte)eSpellType.SavageSlashResistanceBuff:
                case (byte)eSpellType.SavageThrustResistanceBuff:
                case (byte)eSpellType.SpiritResistBuff:
                case (byte)eSpellType.StrengthBuff:
                case (byte)eSpellType.StrengthConstitutionBuff:
                case (byte)eSpellType.SuperiorCourageBuff:
                case (byte)eSpellType.ToHitBuff:
                case (byte)eSpellType.WeaponSkillBuff:
                case (byte)eSpellType.DamageAdd:
                case (byte)eSpellType.OffensiveProc:
                case (byte)eSpellType.DefensiveProc:
                case (byte)eSpellType.DamageShield:
                    {
                        // Buff self, if not in melee, but not each and every mob
                        // at the same time, because it looks silly.
                        if (!LivingHasEffect(Body, spell) && !Body.attackComponent.AttackState && Util.Chance(40) && spell.Target.ToLower() != "pet")
                        {
                            Body.TargetObject = Body;
                            break;
                        }
                        if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && Util.Chance(40) && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && !LivingHasEffect(Body.ControlledBrain.Body, spell) && spell.Target.ToLower() != "self")
                        {
                            Body.TargetObject = Body.ControlledBrain.Body;
                            break;
                        }
                        break;
                    }
                #endregion Buffs

                #region Disease Cure/Poison Cure/Summon
                case (byte)eSpellType.CureDisease:
                    if (Body.IsDiseased)
                    {
                        Body.TargetObject = Body;
                        break;
                    }
                    if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && Body.ControlledBrain.Body.IsDiseased
                        && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && spell.Target.ToLower() != "self")
                    {
                        Body.TargetObject = Body.ControlledBrain.Body;
                        break;
                    }
                    break;
                case (byte)eSpellType.CurePoison:
                    if (LivingIsPoisoned(Body))
                    {
                        Body.TargetObject = Body;
                        break;
                    }
                    if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && LivingIsPoisoned(Body.ControlledBrain.Body)
                        && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && spell.Target.ToLower() != "self")
                    {
                        Body.TargetObject = Body.ControlledBrain.Body;
                        break;
                    }
                    break;
                case (byte)eSpellType.Summon:
                    Body.TargetObject = Body;
                    break;
                case (byte)eSpellType.SummonMinion:
                    //If the list is null, lets make sure it gets initialized!
                    if (Body.ControlledNpcList == null)
                        Body.InitControlledBrainArray(2);
                    else
                    {
                        //Let's check to see if the list is full - if it is, we can't cast another minion.
                        //If it isn't, let them cast.
                        IControlledBrain[] icb = Body.ControlledNpcList;
                        int numberofpets = 0;
                        for (int i = 0; i < icb.Length; i++)
                        {
                            if (icb[i] != null)
                                numberofpets++;
                        }
                        if (numberofpets >= icb.Length)
                            break;
                    }
                    Body.TargetObject = Body;
                    break;
                #endregion Disease Cure/Poison Cure/Summon

                #region Heals
                case (byte)eSpellType.CombatHeal:
                case (byte)eSpellType.Heal:
                case (byte)eSpellType.HealOverTime:
                case (byte)eSpellType.MercHeal:
                case (byte)eSpellType.OmniHeal:
                case (byte)eSpellType.PBAoEHeal:
                case (byte)eSpellType.SpreadHeal:
                    if (spell.Target.ToLower() == "self")
                    {
                        // if we have a self heal and health is less than 75% then heal, otherwise return false to try another spell or do nothing
                        if (Body.HealthPercent < DOL.GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD)
                        {
                            Body.TargetObject = Body;
                        }
                        break;
                    }

                    // Chance to heal self when dropping below 30%, do NOT spam it.
                    if (Body.HealthPercent < (DOL.GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD / 2.0)
                        && Util.Chance(10) && spell.Target.ToLower() != "pet")
                    {
                        Body.TargetObject = Body;
                        break;
                    }

                    if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null
                        && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range
                        && Body.ControlledBrain.Body.HealthPercent < DOL.GS.ServerProperties.Properties.NPC_HEAL_THRESHOLD
                        && spell.Target.ToLower() != "self")
                    {
                        Body.TargetObject = Body.ControlledBrain.Body;
                        break;
                    }
                    break;
                #endregion

                //case "SummonAnimistFnF":
                //case "SummonAnimistPet":
                case (byte)eSpellType.SummonCommander:
                case (byte)eSpellType.SummonDruidPet:
                case (byte)eSpellType.SummonHunterPet:
                case (byte)eSpellType.SummonNecroPet:
                case (byte)eSpellType.SummonUnderhill:
                case (byte)eSpellType.SummonSimulacrum:
                case (byte)eSpellType.SummonSpiritFighter:
                    //case "SummonTheurgistPet":
                    if (Body.ControlledBrain != null)
                        break;
                    Body.TargetObject = Body;
                    break;

                default:
                    //log.Warn($"CheckDefensiveSpells() encountered an unknown spell type [{spell.SpellType}]");
                    break;
            }

            if (Body.TargetObject != null && (spell.Duration == 0 || (Body.TargetObject is GameLiving living && LivingHasEffect(living, spell) == false)))
            {
                casted = Body.CastSpell(spell, m_mobSpellLine);

                if (casted && spell.CastTime > 0)
                {
                    if (Body.IsMoving)
                        Body.StopFollowing();

                    if (Body.TargetObject != Body)
                        Body.TurnTo(Body.TargetObject);
                }
            }

            Body.TargetObject = lastTarget;

            return casted;
        }

        /// <summary>
        /// Checks offensive spells.  Handles dds, debuffs, etc.
        /// </summary>
        protected virtual bool CheckOffensiveSpells(Spell spell)
        {
            if (spell.Target.ToLower() != "enemy" && spell.Target.ToLower() != "area" && spell.Target.ToLower() != "cone")
                return false;

            bool casted = false;

            if (Body.TargetObject is GameLiving living && (spell.Duration == 0 || (!LivingHasEffect(living,spell) || spell.SpellType == (byte)eSpellType.DirectDamageWithDebuff || spell.SpellType == (byte)eSpellType.DamageSpeedDecrease)))
            {
                // Offensive spells require the caster to be facing the target
                if (Body.TargetObject != Body)
                    Body.TurnTo(Body.TargetObject);

                casted = Body.CastSpell(spell, m_mobSpellLine);

                // if (casted && spell.CastTime > 0 && Body.IsMoving)
                //Stopfollowing if spell casted and the cast time > 0 (non-instant spells)
                if (casted && spell.CastTime > 0)
                    Body.StopFollowing();
                //If instant cast and spell casted, and current follow target is not the target object, then switch follow target to current TargetObject
                else if(casted && (spell.CastTime == 0 && Body.CurrentFollowTarget != Body.TargetObject))
                {
                    Body.Follow(Body.TargetObject, GameNPC.STICKMINIMUMRANGE, GameNPC.STICKMAXIMUMRANGE);
                }
            }
            return casted;
        }

        /// <summary>
        /// Checks Instant Spells.  Handles Taunts, shouts, stuns, etc.
        /// </summary>
        protected virtual bool CheckInstantSpells(Spell spell)
        {
            GameObject lastTarget = Body.TargetObject;
            Body.TargetObject = null;

            switch (spell.SpellType)
            {
                #region Enemy Spells
                case (byte)eSpellType.DirectDamage:
                case (byte)eSpellType.Lifedrain:
                case (byte)eSpellType.DexterityDebuff:
                case (byte)eSpellType.StrengthConstitutionDebuff:
                case (byte)eSpellType.CombatSpeedDebuff:
                case (byte)eSpellType.DamageOverTime:
                case (byte)eSpellType.MeleeDamageDebuff:
                case (byte)eSpellType.AllStatsPercentDebuff:
                case (byte)eSpellType.CrushSlashThrustDebuff:
                case (byte)eSpellType.EffectivenessDebuff:
                case (byte)eSpellType.Disease:
                case (byte)eSpellType.Stun:
                case (byte)eSpellType.Mez:
                case (byte)eSpellType.Taunt:
                    if (!LivingHasEffect(lastTarget as GameLiving, spell))
                    {
                        Body.TargetObject = lastTarget;
                    }
                    break;
                #endregion

                #region Combat Spells
                case (byte)eSpellType.CombatHeal:
                case (byte)eSpellType.DamageAdd:
                case (byte)eSpellType.ArmorFactorBuff:
                case (byte)eSpellType.DexterityQuicknessBuff:
                case (byte)eSpellType.EnduranceRegenBuff:
                case (byte)eSpellType.CombatSpeedBuff:
                case (byte)eSpellType.AblativeArmor:
                case (byte)eSpellType.Bladeturn:
                case (byte)eSpellType.OffensiveProc:
                    if (!LivingHasEffect(Body, spell))
                    {
                        Body.TargetObject = Body;
                    }
                    break;
                    #endregion
            }

            if (Body.TargetObject != null && (spell.Duration == 0 || (Body.TargetObject is GameLiving living && LivingHasEffect(living, spell) == false)))
            {
                Body.CastSpell(spell, m_mobSpellLine);
                Body.TargetObject = lastTarget;
                return true;
            }

            Body.TargetObject = lastTarget;
            return false;
        }

        protected static SpellLine m_mobSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);

        /// <summary>
        /// Checks if the living target has a spell effect
        /// </summary>
        /// <param name="target">The target living object</param>
        /// <param name="spell">The spell to check</param>
        /// <returns>True if the living has the effect</returns>
        public static bool LivingHasEffect(GameLiving target, Spell spell)
        {
            if (target == null)
                return true;

            /* all my homies hate vampires
            if (target is GamePlayer && (target as GamePlayer).CharacterClass.ID == (int)eCharacterClass.Vampiir)
            {
                switch (spell.SpellType)
                {
                    case (byte)eSpellType.StrengthConstitutionBuff:
                    case (byte)eSpellType.DexterityQuicknessBuff:
                    case (byte)eSpellType.StrengthBuff:
                    case (byte)eSpellType.DexterityBuff:
                    case (byte)eSpellType.ConstitutionBuff:
                    case (byte)eSpellType.AcuityBuff:
                        return true;
                }
            }*/

            // May not be the right place for that, but without that check NPCs with more than one offensive or defensive proc will only buff themselves once.
            if (spell.SpellType is (byte)eSpellType.OffensiveProc or (byte)eSpellType.DefensiveProc)
            {
                if (target.effectListComponent.Effects.TryGetValue(EffectService.GetEffectFromSpell(spell), out List<ECSGameEffect> existingEffects))
                {
                    if (existingEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == spell.ID || (spell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == spell.EffectGroup)) != null)
                        return true;
                }
            }
            else if (EffectListService.GetEffectOnTarget(target, EffectService.GetEffectFromSpell(spell)) != null)
                return true;

            return false;
        }

        protected bool LivingIsPoisoned(GameLiving target)
        {
            foreach (IGameEffect effect in target.EffectList)
            {
                //If the effect we are checking is not a gamespelleffect keep going
                if (effect is GameSpellEffect == false)
                    continue;

                GameSpellEffect speffect = effect as GameSpellEffect;

                // if this is a DOT then target is poisoned
                if (speffect.Spell.SpellType == (byte)eSpellType.DamageOverTime)
                    return true;
            }

            return false;
        }

        #endregion

        #region Random Walk

        public virtual bool CanRandomWalk {
            get {
                /* Roaming:
				   <0 means random range
				   0 means no roaming
				   >0 means range of roaming
				   defaut roaming range is defined in CanRandomWalk method
				 */
                if (!DOL.GS.ServerProperties.Properties.ALLOW_ROAM)
                    return false;
                if (Body.RoamingRange == 0)
                    return false;
                if (!string.IsNullOrWhiteSpace(Body.PathID))
                    return false;
                return true;
            }
        }

        public virtual IPoint3D CalcRandomWalkTarget()
        {
            int maxRoamingRadius = Body.CurrentRegion.IsDungeon ? 5 : 500;

            if (Body.RoamingRange > 0)
                maxRoamingRadius = Body.RoamingRange;

            double targetX = Body.SpawnPoint.X + Util.Random(-maxRoamingRadius, maxRoamingRadius);
            double targetY = Body.SpawnPoint.Y + Util.Random(-maxRoamingRadius, maxRoamingRadius);

            return new Point3D((int)targetX, (int)targetY, Body.SpawnPoint.Z);
        }

        #endregion

        #region DetectDoor

        public virtual void DetectDoor()
        {
            ushort range = (ushort)((ThinkInterval / 800) * Body.CurrentWayPoint.MaxSpeed);

            foreach (IDoor door in Body.CurrentRegion.GetDoorsInRadius(Body.X, Body.Y, Body.Z, range, false))
            {
                if (door is GameKeepDoor)
                {
                    if (Body.Realm != door.Realm) return;
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
