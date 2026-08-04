using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;

namespace DOL.GS
{
    /// <summary>
    /// In-memory roster and scaling snapshot of a raid encounter, taken when the body enters aggro and discarded when the fight ends.
    /// The roster is keyed by character internal ID so that reconnecting players are still part of the encounter.
    /// </summary>
    public class RaidEncounter
    {
        private static readonly Lock _activeEncountersLock = new();
        private static readonly HashSet<RaidEncounter> _activeEncounters = new();
        private static volatile RaidEncounter[] _activeEncounterSnapshot = [];

        // Grants owed to participants who were logged out when their encounter died. Not persisted: a restart voids them.
        private static readonly Lock _pendingGrantsLock = new();
        private static readonly Dictionary<string, List<PendingGrant>> _pendingGrants = new();

        private readonly HashSet<string> _roster = new();
        private readonly Dictionary<string, long> _lastActivity = new();
        private readonly Dictionary<string, long> _quitTimes = new();
        private readonly Lock _activityLock = new();

        private readonly List<GameNPC> _adds = new();
        private readonly Lock _addsLock = new();

        private readonly record struct PendingReward(int BountyPoints, string CurrencyItemTemplateId, int CurrencyItemCount, string SourceName);

        private readonly record struct PendingGrant(long EnqueuedAt, Action<GamePlayer> Grant);

        public RaidEncounter(StandardMobBrain owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// The brain that drives the encounter's lifecycle. Brains of linked adds share the instance but never snapshot or clear it.
        /// </summary>
        public StandardMobBrain Owner { get; }

        public bool Active { get; private set; }
        public int Size { get; set; }
        public int ScaleSize => Math.Clamp(Size, Properties.RAID_SCALING_BASELINE_SIZE, Properties.RAID_SCALING_MAX_SIZE);
        public double HpMultiplier => 1 + Properties.RAID_SCALING_HP_PER_EXTRA_PLAYER * (ScaleSize - Properties.RAID_SCALING_BASELINE_SIZE);

        /// <summary>
        /// Bounty points granted to every present roster member on a kill. 0 disables the grant.
        /// </summary>
        public int BountyPointsReward { get; set; }

        /// <summary>
        /// Id_nb of the currency item granted to every present roster member on a kill. Null disables the grant.
        /// </summary>
        public string CurrencyItemTemplateId { get; set; }

        /// <summary>
        /// How many copies of <see cref="CurrencyItemTemplateId"/> every present roster member receives.
        /// </summary>
        public int CurrencyItemCount { get; set; }

        /// <summary>
        /// Loot table rolls the roster size is worth on top of the regular drop.
        /// </summary>
        public int BonusLootRolls
        {
            get
            {
                int itemShareSize = Properties.RAID_SCALING_ITEM_SHARE_SIZE;
                return itemShareSize > 0 ? Math.Max(0, (ScaleSize - Properties.RAID_SCALING_BASELINE_SIZE) / itemShareSize) : 0;
            }
        }

        /// <summary>
        /// How many units the encounter is worth at one unit per <paramref name="playersPerUnit"/> roster members.
        /// Inactive encounters are worth the baseline raid size. <paramref name="maxCount"/> is mandatory so each
        /// spawn site chooses its own cap.
        /// </summary>
        public int ScaleUnitCount(int playersPerUnit, int maxCount)
        {
            if (playersPerUnit <= 0)
                return maxCount;

            int size = Active ? ScaleSize : Properties.RAID_SCALING_BASELINE_SIZE;
            return Math.Min(maxCount, Math.Max(1, size / playersPerUnit));
        }

        /// <summary>
        /// Locks the roster and computes the scaling factors. The roster never changes afterwards.
        /// </summary>
        /// <returns>True if the encounter became active.</returns>
        public bool Snapshot(GameNPC body, StandardMobBrain brain)
        {
            GamePlayer puller = null;

            foreach (GameLiving living in brain.GetOrderedAggroList())
            {
                if (living is GamePlayer player)
                {
                    puller = player;
                    break;
                }
            }

            if (puller == null)
                return false;

            HashSet<string> newRoster = new();
            List<GamePlayer> members = new();
            newRoster.Add(puller.InternalID);
            members.Add(puller);
            BattleGroup battleGroup = puller.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);

            if (battleGroup != null)
            {
                foreach (GamePlayer member in battleGroup.Members.Keys)
                {
                    if (member != null && member.CurrentRegionID == puller.CurrentRegionID && newRoster.Add(member.InternalID))
                        members.Add(member);
                }
            }
            else if (puller.Group != null)
            {
                foreach (GamePlayer member in puller.Group.GetPlayersInTheGroup())
                {
                    if (member != null && newRoster.Add(member.InternalID))
                        members.Add(member);
                }
            }

            foreach (GamePlayer player in body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (newRoster.Add(player.InternalID))
                    members.Add(player);
            }

            lock (_activityLock)
            {
                _roster.Clear();

                foreach (string internalId in newRoster)
                    _roster.Add(internalId);

                Size = newRoster.Count;
                Active = true;
            }

            lock (_activeEncountersLock)
            {
                if (_activeEncounters.Add(this))
                    _activeEncounterSnapshot = _activeEncounters.ToArray();
            }

            StripOutsiderBuffs(members, newRoster);
            return true;
        }

        private void StripOutsiderBuffs(List<GamePlayer> members, HashSet<string> roster)
        {
            foreach (GamePlayer member in members)
            {
                foreach (ECSGameEffect effect in member.effectListComponent.GetEffects())
                {
                    ISpellHandler spellHandler = effect.SpellHandler;

                    if (spellHandler == null || spellHandler.Spell.IsHarmful)
                        continue;

                    GameLiving effectCaster = spellHandler.Caster;

                    if (effectCaster is GameNPC npc && npc.Brain is IControlledBrain controlledBrain)
                        effectCaster = controlledBrain.GetPlayerOwner();

                    if (effectCaster is GamePlayer playerCaster && !roster.Contains(playerCaster.InternalID))
                        effect.End();
                }
            }
        }

        /// <summary>
        /// Whether any active encounter has the target on its roster while the caster is not.
        /// </summary>
        public static bool IsBlockedFromHelping(GamePlayer caster, GamePlayer target)
        {
            RaidEncounter[] encounters = _activeEncounterSnapshot;

            if (encounters.Length == 0 || caster == null || target == null || caster == target)
                return false;

            foreach (RaidEncounter encounter in encounters)
            {
                if (encounter.Owner.Body is not { IsAlive: true, ObjectState: GameObject.eObjectState.Active })
                    continue;

                if (encounter.BlocksHelp(caster, target))
                    return true;
            }

            return false;
        }

        private bool BlocksHelp(GamePlayer caster, GamePlayer target)
        {
            lock (_activityLock)
            {
                return _roster.Contains(target.InternalID) && !_roster.Contains(caster.InternalID);
            }
        }

        /// <summary>
        /// Whether at least one encounter is currently registered. Cheap gate for the recording entry points.
        /// </summary>
        public static bool HasActiveEncounters => _activeEncounterSnapshot.Length > 0;

        /// <summary>
        /// Copy of the currently registered encounters.
        /// </summary>
        public static List<RaidEncounter> GetActiveEncounters()
        {
            return new List<RaidEncounter>(_activeEncounterSnapshot);
        }

        /// <summary>
        /// Stamps the caster as active on every active encounter that has both the caster and the target of a beneficial action on its roster.
        /// </summary>
        public static void RecordHelpActivity(GamePlayer caster, GamePlayer target)
        {
            RaidEncounter[] encounters = _activeEncounterSnapshot;

            if (encounters.Length == 0 || caster == null || target == null || caster == target)
                return;

            foreach (RaidEncounter encounter in encounters)
            {
                lock (encounter._activityLock)
                {
                    if (encounter._roster.Contains(target.InternalID) && encounter._roster.Contains(caster.InternalID))
                        encounter._lastActivity[caster.InternalID] = GameLoop.GameLoopTime;
                }
            }
        }

        /// <summary>
        /// Stamps the caster as active on the encounter the targeted NPC belongs to, covering crowd control and debuffs that don't necessarily register as attacks.
        /// </summary>
        public static void RecordHostileSupportActivity(GamePlayer caster, GameNPC target)
        {
            if (!HasActiveEncounters || caster == null || target == null)
                return;

            if ((target.Brain as StandardMobBrain)?.RaidEncounter is { Active: true } encounter)
                encounter.RecordActivity(caster);
        }

        /// <summary>
        /// Stamps the character as logged out on every encounter they took part in, so that a kill within
        /// <see cref="Properties.RAID_SCALING_QUIT_GRACE_MINUTES"/> minutes still pays them out on their next login.
        /// </summary>
        public static void OnPlayerQuit(GamePlayer player)
        {
            if (player == null)
                return;

            foreach (RaidEncounter encounter in _activeEncounterSnapshot)
                encounter.RecordQuit(player.InternalID);
        }

        /// <summary>
        /// Clears the character's logout stamps, since they're resolvable live again, and hands over the rewards
        /// of any encounter that died while they were logged out.
        /// </summary>
        public static void OnPlayerEnterWorld(GamePlayer player)
        {
            if (player == null)
                return;

            foreach (RaidEncounter encounter in _activeEncounterSnapshot)
                encounter.ClearQuit(player.InternalID);

            DeliverPendingRewards(player);
        }

        private void RecordQuit(string internalId)
        {
            if (string.IsNullOrEmpty(internalId))
                return;

            lock (_activityLock)
            {
                if (_lastActivity.ContainsKey(internalId))
                    _quitTimes[internalId] = GameLoop.GameLoopTime;
            }
        }

        private void ClearQuit(string internalId)
        {
            if (string.IsNullOrEmpty(internalId))
                return;

            lock (_activityLock)
                _quitTimes.Remove(internalId);
        }

        /// <summary>
        /// Records that a roster member contributed to the encounter, keeping them counted as an active combatant until they age out of the activity window.
        /// </summary>
        public void RecordActivity(GamePlayer player)
        {
            if (player == null)
                return;

            lock (_activityLock)
            {
                if (_roster.Contains(player.InternalID))
                    _lastActivity[player.InternalID] = GameLoop.GameLoopTime;
            }
        }

        /// <summary>
        /// Whether the character contributed to the encounter at least once. Keyed by internal id so the answer
        /// survives the player going linkdead or logging off.
        /// </summary>
        public bool HasParticipated(string internalId)
        {
            if (string.IsNullOrEmpty(internalId))
                return false;

            lock (_activityLock)
            {
                return _lastActivity.ContainsKey(internalId);
            }
        }

        /// <summary>
        /// Roster members who attacked, healed, buffed or debuffed within the last <see cref="Properties.RAID_SCALING_ACTIVITY_WINDOW_SECONDS"/> seconds.
        /// </summary>
        public int GetActiveAttackerCount()
        {
            long threshold = GameLoop.GameLoopTime - Properties.RAID_SCALING_ACTIVITY_WINDOW_SECONDS * 1000L;
            int count = 0;

            lock (_activityLock)
            {
                foreach (long lastSeen in _lastActivity.Values)
                {
                    if (lastSeen >= threshold)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Roster member count.
        /// </summary>
        public int RosterCount
        {
            get
            {
                lock (_activityLock)
                    return _roster.Count;
            }
        }

        /// <summary>
        /// How many roster members contributed at least once.
        /// </summary>
        public int ParticipantCount
        {
            get
            {
                lock (_activityLock)
                    return _lastActivity.Count;
            }
        }

        /// <summary>
        /// Roster members who contributed at least once, resolved to their live player. Linkdead players are still
        /// resolvable until their quit timer fires; participants who fully logged out are absent from the result.
        /// </summary>
        public List<GamePlayer> GetParticipants()
        {
            List<GamePlayer> participants = new();

            foreach (GamePlayer player in ClientService.Instance.GetPlayers())
            {
                if (HasParticipated(player.InternalID))
                    participants.Add(player);
            }

            return participants;
        }

        /// <summary>
        /// Copy of every roster member's internal id.
        /// </summary>
        public List<string> GetRosterIds()
        {
            lock (_activityLock)
            {
                return new List<string>(_roster);
            }
        }

        /// <summary>
        /// Raises the armor factor above its default by the share of expected attackers that aren't currently attacking.
        /// </summary>
        public double CalculateArmorFactorScalingFactor(double defaultArmorFactorScalingFactor, int activeAttackerCount)
        {
            double expectedActive = Properties.RAID_SCALING_ACTIVE_FRACTION * ScaleSize;

            if (expectedActive <= 0)
                return defaultArmorFactorScalingFactor;

            double deficit = Math.Max(0, expectedActive - activeAttackerCount);
            return defaultArmorFactorScalingFactor * (1 + Properties.RAID_SCALING_AF_IDLE_WEIGHT * deficit / expectedActive);
        }

        /// <summary>
        /// Runs the grant on every live participant now, and at next login for participants who quit
        /// within the grace window. Returns the live participants the grant ran on.
        /// </summary>
        public List<GamePlayer> GrantToParticipants(Action<GamePlayer> grant)
        {
            List<GamePlayer> live = GetParticipants();
            HashSet<string> paid = new();

            foreach (GamePlayer player in live)
            {
                grant(player);
                paid.Add(player.InternalID);
            }

            long graceThreshold = GameLoop.GameLoopTime - Properties.RAID_SCALING_QUIT_GRACE_MINUTES * 60000L;
            List<string> owed = new();

            lock (_activityLock)
            {
                foreach (string internalId in _lastActivity.Keys)
                {
                    if (!paid.Contains(internalId) && _quitTimes.TryGetValue(internalId, out long quitTime) && quitTime >= graceThreshold)
                        owed.Add(internalId);
                }
            }

            if (owed.Count > 0)
            {
                lock (_pendingGrantsLock)
                {
                    SweepExpiredPendingGrants(graceThreshold);

                    foreach (string internalId in owed)
                    {
                        if (!_pendingGrants.TryGetValue(internalId, out List<PendingGrant> grants))
                        {
                            grants = new();
                            _pendingGrants[internalId] = grants;
                        }

                        grants.Add(new PendingGrant(GameLoop.GameLoopTime, grant));
                    }
                }
            }

            return live;
        }

        /// <summary>
        /// Hands the configured personal rewards to every participant, wherever they are — the encounter is the
        /// source of truth, so dying, releasing or going linkdead before the kill doesn't forfeit the reward.
        /// </summary>
        public void GrantPersonalRewards(GameNPC body)
        {
            if (!Active)
                return;

            DbItemTemplate currencyTemplate = CurrencyItemCount > 0 && !string.IsNullOrEmpty(CurrencyItemTemplateId)
                ? GameServer.Database.FindObjectByKey<DbItemTemplate>(CurrencyItemTemplateId)
                : null;

            if (BountyPointsReward <= 0 && currencyTemplate == null)
                return;

            PendingReward reward = new(BountyPointsReward, CurrencyItemTemplateId, CurrencyItemCount, body.GetName(0, false));
            string logSource = InventoryLogging.GetGameObjectString(body);
            GrantToParticipants(player => GrantPersonalReward(player, reward, currencyTemplate, logSource));
        }

        /// <summary>
        /// Drops every pending grant older than the grace window. Callers must hold <see cref="_pendingGrantsLock"/>.
        /// </summary>
        private static void SweepExpiredPendingGrants(long threshold)
        {
            List<string> emptied = null;

            foreach (var pair in _pendingGrants)
            {
                pair.Value.RemoveAll(grant => grant.EnqueuedAt < threshold);

                if (pair.Value.Count == 0)
                    (emptied ??= new()).Add(pair.Key);
            }

            if (emptied == null)
                return;

            foreach (string internalId in emptied)
                _pendingGrants.Remove(internalId);
        }

        private static void DeliverPendingRewards(GamePlayer player)
        {
            List<PendingGrant> grants;

            lock (_pendingGrantsLock)
            {
                if (!_pendingGrants.Remove(player.InternalID, out grants))
                    return;
            }

            long threshold = GameLoop.GameLoopTime - Properties.RAID_SCALING_QUIT_GRACE_MINUTES * 60000L;

            foreach (PendingGrant grant in grants)
            {
                if (grant.EnqueuedAt >= threshold)
                    grant.Grant(player);
            }
        }

        private static void GrantPersonalReward(GamePlayer player, PendingReward reward, DbItemTemplate currencyTemplate, string logSource)
        {
            if (reward.BountyPoints > 0)
                player.GainBountyPoints(reward.BountyPoints);

            if (currencyTemplate == null)
                return;

            // A stackable currency is handed over as one stack, anything else as one item per copy.
            int copies = currencyTemplate.IsStackable ? 1 : reward.CurrencyItemCount;
            int countPerCopy = currencyTemplate.IsStackable ? reward.CurrencyItemCount : 1;
            bool delivered = true;

            for (int copy = 0; copy < copies; copy++)
            {
                WorldInventoryItem currency = WorldInventoryItem.CreateFromTemplate(currencyTemplate);

                if (currency?.Item == null)
                    continue;

                currency.Item.Count = countPerCopy;
                currency.Item.Creator = reward.SourceName;
                delivered &= DeliverCurrency(player, currency, logSource);
            }

            if (delivered)
                player.Out.SendMessage($"You receive {reward.CurrencyItemCount} {currencyTemplate.Name} from {reward.SourceName}.", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            else
                player.Out.SendMessage($"Your inventory is full. The {currencyTemplate.Name} from {reward.SourceName} was dropped at your feet.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Hands the item to the player's backpack, falling back to a drop reserved for them at their feet.
        /// </summary>
        private static bool DeliverCurrency(GamePlayer player, WorldInventoryItem worldItem, string logSource)
        {
            if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, worldItem.Item))
            {
                InventoryLogging.LogInventoryAction(logSource, player, eInventoryActionType.Loot, worldItem.Item.Template, worldItem.Item.Count);
                return true;
            }

            worldItem.AddOwner(player);
            worldItem.X = player.X;
            worldItem.Y = player.Y;
            worldItem.Z = player.Z;
            worldItem.Heading = player.Heading;
            worldItem.CurrentRegion = player.CurrentRegion;
            worldItem.AddToWorld();
            return false;
        }

        public bool IsOnRoster(GamePlayer player)
        {
            if (player == null)
                return false;

            lock (_activityLock)
            {
                return _roster.Contains(player.InternalID);
            }
        }

        public bool AddToRoster(GamePlayer player)
        {
            if (player == null)
                return false;

            HashSet<string> roster;

            lock (_activityLock)
            {
                if (!_roster.Add(player.InternalID))
                    return false;

                Size++;
                roster = new(_roster);
            }

            StripOutsiderBuffs([player], roster);
            return true;
        }

        public bool RemoveFromRoster(GamePlayer player)
        {
            if (player == null)
                return false;

            lock (_activityLock)
            {
                _lastActivity.Remove(player.InternalID);
                _quitTimes.Remove(player.InternalID);

                if (!_roster.Remove(player.InternalID))
                    return false;

                Size--;
                return true;
            }
        }

        /// <summary>
        /// Ties an encounter-spawned add to the encounter's lifecycle: it is despawned when the encounter ends or resets.
        /// Adds that die or leave the world on their own simply fall out of the registry.
        /// </summary>
        public void RegisterAdd(GameNPC add)
        {
            if (add == null)
                return;

            lock (_addsLock)
            {
                _adds.RemoveAll(static existing => !existing.IsAlive || existing.ObjectState is not GameObject.eObjectState.Active);
                _adds.Add(add);
            }
        }

        /// <summary>
        /// Removes every registered add that is still alive and in the world. Runs as part of <see cref="Clear"/>,
        /// so it fires whenever the encounter ends or resets; owner brains with an earlier reset of their own may also call it directly.
        /// </summary>
        public void DespawnAdds()
        {
            lock (_addsLock)
            {
                foreach (GameNPC add in _adds)
                {
                    if (add.IsAlive && add.ObjectState is GameObject.eObjectState.Active)
                        add.RemoveFromWorld();
                }

                _adds.Clear();
            }
        }

        public void Clear()
        {
            DespawnAdds();

            lock (_activeEncountersLock)
            {
                if (_activeEncounters.Remove(this))
                    _activeEncounterSnapshot = _activeEncounters.ToArray();
            }

            lock (_activityLock)
            {
                _roster.Clear();
                _lastActivity.Clear();
                _quitTimes.Clear();
                Active = false;
                Size = 0;
            }

            GameNPC body = Owner.Body;

            if (body != null && body.Health > body.MaxHealth)
                body.Health = body.MaxHealth;
        }
    }
}
