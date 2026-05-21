using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.GS.Styles;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using DOL.Logging;

namespace DOL.GS
{
    /// <summary>
    /// Manages the recording, storage, and execution of player-defined macros called "recorders".
    /// Provides methods to start/stop recordings, save them to the database, execute them as spells,
    /// and manage the recorder spellbook. Recorders let players record multiple spells, abilities, 
    /// styles, and commands in sequence and replay them via a single macro button on their quickbar.
    /// </summary>
    public class RecorderMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        #region Constants
        public const string RecorderLineKey = "Recorder";
        public const string RecorderDisplayName = "Recorder";
        public const int RecorderBaseIcon = 11130;      // Default icon: Animist DD

        private const int SpellIdBase = 100_000;        // Base offset for macro spell IDs
        private const int ListPreviewActionCount = 3;   // Max actions shown in /recorder list
        private const int ImportThrottleMilliseconds = 3000; // Min time between imports per player

        // Cached string for spell type to avoid ToString() allocation per build
        private static readonly string RecorderSpellTypeString = eSpellType.RecorderAction.ToString();
        #endregion

        #region Async Import Throttling
        // Lock-free tracking using ConcurrentDictionary. Prevents concurrent imports per player
        // and throttles rapid successive imports. All operations are atomic/non-blocking.
        private static readonly ConcurrentDictionary<string, DateTime> _lastImportTime = new();
        #endregion

        #region Initialization
        public static bool Init()
        {
            if (!Properties.ENABLE_RECORDER)
            {
                log.Info("[RECORDER] System disabled via server property 'enable_recorder'.");
                return false;
            }
            log.Debug("[RECORDER] System fully initialized.");
            return true;
        }
        #endregion

        #region Core Logic
        /// <summary>
        /// Rebuilds the player's recorder spellbook from the in-memory
        /// <see cref="GamePlayer.RecorderDbEntries"/> cache (no DB query).
        /// Called mid-session after any create/delete/update operation so the
        /// spellbook stays in sync with the cached rows.
        /// </summary>
        public static void RefreshPlayerRecorders(GamePlayer player)
        {
            if (player?.Client == null) return;
            BuildPlayerRecorders(player, player.RecorderDbEntries ?? []);
        }

        /// <summary>
        /// Builds the player's recorder spell macros from the supplied DB rows and pushes
        /// the updated spellbook to the client. Called both from the async login path
        /// (rows already loaded by <c>GamePlayer.LoadFromDatabaseAsync</c>) and from
        /// <see cref="RefreshPlayerRecorders"/> during mid-session updates.
        /// </summary>
        public static void BuildPlayerRecorders(GamePlayer player, IList<DBCharacterRecorder> dbEntries)
        {
            if (player?.Client == null) return;

            try
            {
                // Sort by auto-increment ID to preserve the order the macros were saved in.
                var ordered = dbEntries.OrderBy(r => r.ID);

                player.SpellMacros.Clear();

                // Reuse one StringBuilder across all entries to avoid per-entry heap allocations.
                var tooltipBuilder = new StringBuilder();

                foreach (var entry in ordered)
                {
                    // Parse JSON once — used for both tooltip and the cached Actions list in RecorderSpell.
                    List<RecorderAction> parsedActions = null;
                    tooltipBuilder.Clear().Append("[Recorder]\n");
                    try
                    {
                        parsedActions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson);
                        if (parsedActions != null)
                        {
                            // GetActionDescription covers all action types in one place —
                            // avoids duplicating formatting logic here.
                            foreach (var action in parsedActions)
                                tooltipBuilder.Append(GetActionDescription(action, player.CharacterClass.ID)).Append("\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        parsedActions = null;
                        tooltipBuilder.Clear().Append("Recorded Macro (Data Error)");
                        log.Error($"[RECORDER] Error parsing actions for {entry.Name}: {ex}");
                    }

                    int uniqueID = SpellIdBase + player.SpellMacros.Count;
                    int uniqueLevel = player.SpellMacros.Count + 1;
                    int tooltipId = player.LastMacroToolTipID++;

                    DbSpell db = new DbSpell
                    {
                        Name = entry.Name,
                        Icon = (ushort)entry.IconID,
                        ClientEffect = (ushort)entry.IconID,
                        SpellID = uniqueID,
                        Target = "Self",
                        CastTime = 0,
                        Type = RecorderSpellTypeString,
                        Description = tooltipBuilder.ToString(),
                        TooltipId = (ushort)tooltipId,
                        Power = 0,
                    };

                    player.SpellMacros.Add(new RecorderSpell(db, uniqueLevel, entry, parsedActions));
                }

                // Add the specialization once if not already present - never remove it,
                // because GetSpellLinesForLiving always returns the line (even when empty)
                // so the client receives a 0-spell packet that clears the slot.
                // AddSpecialization is a no-op if the key already exists.
                player.AddSpecialization(new RecorderSpecialization(RecorderLineKey, RecorderDisplayName, 1));

                // SendUpdatePlayerSkills internally calls GetAllUsableListSpells(true) and
                // SendNonHybridSpellLines, which together push the updated (possibly empty)
                // Recorder line to the client so the slot is properly cleared.
                player.Out.SendUpdatePlayerSkills(true);
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER REFRESH ERROR] {ex}");
            }
        }
        #endregion

        #region Helper Classes
        public class RecorderSpell : Spell
        {
            public DBCharacterRecorder RecordData { get; }
            /// <summary>Pre-parsed actions list cached at spellbook build time — avoids re-parsing JSON on every cast.</summary>
            public List<RecorderAction> Actions { get; }
            public RecorderSpell(DbSpell db, int level, DBCharacterRecorder data, List<RecorderAction> actions)
                : base(db, level)
            {
                RecordData = data;
                Actions = actions;
            }
        }

        public class RecorderSpecialization : Specialization
        {
            private readonly SpellLine m_line;
            // Cache the single-element list so GetSpellLinesForLiving never allocates.
            private readonly List<SpellLine> m_lineList;

            public RecorderSpecialization(string keyname, string displayname, ushort icon) : base(keyname, displayname, icon)
            {
                m_line = new SpellLine(RecorderLineKey, RecorderLineKey, RecorderLineKey, false);
                m_lineList = new List<SpellLine> { m_line };
            }

            public override bool Trainable => false;

            public override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living)
            {
                Dictionary<SpellLine, List<Skill>> dict = new();
                if (living is GamePlayer player && player.SpellMacros?.Count > 0)
                {
                    // RecorderSpell extends Spell which extends Skill — no cast overhead needed.
                    dict.Add(m_line, new List<Skill>(player.SpellMacros));
                }
                return dict;
            }

            public override List<SpellLine> GetSpellLinesForLiving(GameLiving living)
            {
                // Always return the line even when empty. When SpellMacros is empty the
                // client receives a 0-spell packet for this slot, which clears any previous
                // content. Without this the client keeps the old ghost entry in the spellbook.
                return m_lineList;
            }
        }
        #endregion

        #region Recording Logic
        public static bool IsPlayerRecording(GamePlayer player)
        {
            return player?.RecorderActions != null;
        }
        public static bool HasPendingIcon(GamePlayer player)
        {
            return player != null && !string.IsNullOrEmpty(player.PendingRecorderIconName);
        }
        public static bool HasPendingInsert(GamePlayer player)
        {
            return player != null && !string.IsNullOrEmpty(player.PendingInsertRecorderName);
        }

        /// <summary>
        /// Checks if adding an action would exceed the max action limit for a recorder.
        /// </summary>
        /// <returns><c>true</c> if the action can be added; <c>false</c> if limit exceeded.</returns>
        private static bool CanAddAction(GamePlayer player, string recorderName = null)
        {
            if (Properties.RECORDER_MAX_ACTIONS <= 0)
                return true; // No limit configured

            // Check current recording session limit
            if (!string.IsNullOrEmpty(recorderName))
            {
                // For insert mode — check the target recorder's action count
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(recorderName, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    try
                    {
                        var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson);
                        if (actions != null && actions.Count >= Properties.RECORDER_MAX_ACTIONS)
                            return false;
                    }
                    catch { return false; }
                }
            }
            else if (player.RecorderActions != null && player.RecorderActions.Count >= Properties.RECORDER_MAX_ACTIONS)
            {
                return false; // Current session recording is at limit
            }

            return true;
        }

        // This gets called from /recorder start command
        public static void StartRecording(GamePlayer player)
        {
            if (player == null) return;

            if (IsPlayerRecording(player))
            {
                player.Out.SendMessage("Already recording. Use /recorder save <name> to save or /recorder cancel to discard.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            // Check per-player recorder limit
            if (Properties.RECORDER_MAX_PER_PLAYER > 0 && player.RecorderDbEntries?.Count >= Properties.RECORDER_MAX_PER_PLAYER)
            {
                player.Out.SendMessage($"You have reached the maximum number of recorders ({Properties.RECORDER_MAX_PER_PLAYER}). Delete one before creating a new one.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.RecorderActions = new List<RecorderAction>();
            player.Out.SendMessage("Recording started. Use /recorder save <name> when done.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// Arms insert mode at the end of an existing recorder.
        /// Equivalent to <see cref="StartInsertMode"/> with index = count + 1.
        /// </summary>
        public static void StartAppendMode(GamePlayer player, string name)
        {
            if (player == null) return;

            var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                player.Out.SendMessage($"Unknown recorder '{name}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            int count;
            try
            {
                var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson);
                count = actions?.Count ?? 0;
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] corrupt action data in '{name}': {ex}");
                player.Out.SendMessage($"[{name}] Action data is corrupt. Contact a GM.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            // Check action limit before allowing append
            if (Properties.RECORDER_MAX_ACTIONS > 0 && count >= Properties.RECORDER_MAX_ACTIONS)
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.PendingInsertRecorderName = name;
            player.PendingInsertRecorderIndex = count + 1;
            player.Out.SendMessage($"Your next action will be appended to [{name}].", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// Arms insert mode: the next action the player casts/uses will be inserted into
        /// <paramref name="name"/> at <paramref name="index"/> (1-based) and saved immediately.
        /// Validates that the recorder exists and the index is in range before arming.
        /// </summary>
        public static void StartInsertMode(GamePlayer player, string name, int index)
        {
            if (player == null) return;

            var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                player.Out.SendMessage($"Unknown recorder '{name}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            List<RecorderAction> actions;
            try
            {
                actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson) ?? new List<RecorderAction>();
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] corrupt action data in '{name}': {ex}");
                player.Out.SendMessage($"[{name}] Action data is corrupt. Contact a GM.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            if (index < 1 || index > actions.Count + 1)
            {
                player.Out.SendMessage($"Invalid index. {name} has {actions.Count} action(s). Valid positions are 1 to {actions.Count + 1}.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            // Check action limit before allowing insert
            if (Properties.RECORDER_MAX_ACTIONS > 0 && actions.Count >= Properties.RECORDER_MAX_ACTIONS)
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.PendingInsertRecorderName = name;
            player.PendingInsertRecorderIndex = index;
            player.Out.SendMessage($"Your next action will be inserted at position {index} in [{name}].", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// If the player has a pending insert armed, inserts <paramref name="action"/> into
        /// the target recorder, saves it, refreshes the spellbook, and clears the pending state.
        /// </summary>
        /// <returns><c>true</c> if the action was consumed by the pending insert (caller should not record it normally).</returns>
        private static bool TryHandlePendingInsert(GamePlayer player, RecorderAction action)
        {
            if (!HasPendingInsert(player))
                return false;

            string name = player.PendingInsertRecorderName;
            int index = player.PendingInsertRecorderIndex;

            // Clear immediately so a DB/parse error doesn't leave the player stuck.
            player.PendingInsertRecorderName = null;
            player.PendingInsertRecorderIndex = 0;

            try
            {
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    player.Out.SendMessage($"Recorder [{name}] no longer exists. Insert cancelled.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return true;
                }

                List<RecorderAction> actions;
                try
                {
                    actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson) ?? new List<RecorderAction>();
                }
                catch (Exception ex)
                {
                    log.Error($"[RECORDER] corrupt action data in '{name}': {ex}");
                    player.Out.SendMessage($"[{name}] Action data is corrupt. Contact a GM.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return true;
                }

                int insertAt = Math.Clamp(index - 1, 0, actions.Count);
                
                // Check action limit before inserting
                if (actions.Count >= Properties.RECORDER_MAX_ACTIONS)
                {
                    player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return true;
                }
                
                actions.Insert(insertAt, action);

                entry.ActionsJson = JsonConvert.SerializeObject(actions);
                entry.Dirty = true;
                GameServer.Database.SaveObject(entry);
                RefreshPlayerRecorders(player);

                string desc = GetActionDescription(action, player.CharacterClass.ID);
                player.Out.SendMessage($"[{name}] {desc} inserted at position {index}.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] error inserting into '{name}' for {player.Name}: {ex}");
            }

            return true;
        }

        // This gets called from CastingComponent/Style..../Ability...
        public static void RecordAction(GamePlayer player, Spell spell)
        {
            if (player == null || spell == null) return;

            // Optionally handle pending icon change before recording action
            if (HasPendingIcon(player))
            {
                var recorderName = player.PendingRecorderIconName;
                // assign icon to that recorder
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(recorderName, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    entry.IconID = spell.Icon;
                    entry.Dirty = true;
                    GameServer.Database.SaveObject(entry);
                    RefreshPlayerRecorders(player);
                    player.Out.SendMessage($"[{recorderName}] Icon set to {spell.Name}.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
                else
                {
                    player.Out.SendMessage($"Recorder '{recorderName}' not found. Icon was not set.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
                player.PendingRecorderIconName = null;
                // The spell that set the icon is not recorded — return here so it isn't
                // also consumed by a pending insert or added to an active recording.
                return;
            }

            // Disable recording recorders
            if (spell is RecorderSpell)
                return;

            var action = new RecorderAction { Type = RecorderActionType.Spell, ID = spell.ID };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

            // Check action limit before adding
            if (!CanAddAction(player))
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            // Record spells/styles/...
            player.RecorderActions.Add(action);
            int pos = player.RecorderActions.Count;
            player.Out.SendMessage($"[{pos}] Spell: {spell.Name}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        public static void RecordAction(GamePlayer player, Style style)
        {
            if (player == null || style == null) return;

            var action = new RecorderAction { Type = RecorderActionType.Style, ID = style.ID };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

            // Check action limit before adding
            if (!CanAddAction(player))
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.RecorderActions.Add(action);
            int pos = player.RecorderActions.Count;
            player.Out.SendMessage($"[{pos}] Style: {style.Name}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        public static void RecordAction(GamePlayer player, Ability ability)
        {
            if (player == null || ability == null) return;

            var action = new RecorderAction { Type = RecorderActionType.Ability, ID = ability.ID };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

            // Check action limit before adding
            if (!CanAddAction(player))
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.RecorderActions.Add(action);
            int pos = player.RecorderActions.Count;
            player.Out.SendMessage($"[{pos}] Ability: {ability.Name}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// Record a player‑entered command while recording is active.
        /// </summary>
        public static void RecordAction(GamePlayer player, string command)
        {
            if (player == null || string.IsNullOrEmpty(command)) return;

            var action = new RecorderAction { Type = RecorderActionType.Command, ID = 0, Value = command };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

            // Check action limit before adding
            if (!CanAddAction(player))
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.RecorderActions.Add(action);
            int pos = player.RecorderActions.Count;
            string displayCmd = command.Length > 0 && command[0] == '&' ? "/" + command[1..] : command;
            player.Out.SendMessage($"[{pos}] Command: {displayCmd}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// Records a weapon-slot switch while recording or insert mode is active.
        /// Stores the inventory <paramref name="slot"/> as <see cref="RecorderAction.ID"/> and
        /// the item's display name as <see cref="RecorderAction.Value"/>.
        /// </summary>
        public static void RecordAction(GamePlayer player, int slot, string itemName)
        {
            if (player == null) return;

            var action = new RecorderAction
            {
                Type  = RecorderActionType.WeaponSwitch,
                ID    = slot,
                Value = itemName
            };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

            // Check action limit before adding
            if (!CanAddAction(player))
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.RecorderActions.Add(action);
            int pos = player.RecorderActions.Count;
            player.Out.SendMessage($"[{pos}] Use: {itemName}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// Records an equipped-item charge use while recording or insert mode is active.
        /// Stores the equipped slot position as <see cref="RecorderAction.ID"/> so replay can
        /// resolve the correct item, and packs the charge type plus spell display name into
        /// <see cref="RecorderAction.Value"/> as <c>"{type}:{spellName}"</c>.
        /// </summary>
        public static void RecordAction(GamePlayer player, DbInventoryItem item, int type)
        {
            if (player == null || item == null) return;

            // Resolve the charge spell name so the feedback and tooltip show what the charge
            // actually does, not which item it came from.
            // UseItemCharge uses the same line, so this is consistent with how charges are cast.
            SpellLine chargeEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
            int spellId = type == 2 ? item.SpellID1 : item.SpellID;
            Spell chargeSpell = SkillBase.FindSpell(spellId, chargeEffectLine);
            string displayName = chargeSpell?.Name ?? item.GetName(0, false);

            var action = new RecorderAction
            {
                Type  = RecorderActionType.ItemCharge,
                ID    = item.SlotPosition,
                Value = $"{type}:{displayName}"
            };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

            // Check action limit before adding
            if (!CanAddAction(player))
            {
                player.Out.SendMessage($"The recorder has reached the maximum number of {Properties.RECORDER_MAX_ACTIONS} actions!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            player.RecorderActions.Add(action);
            int pos = player.RecorderActions.Count;
            player.Out.SendMessage($"[{pos}] Item Use: {displayName}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        public static void StopAndSaveRecording(GamePlayer player, string name)
        {
            if (player?.RecorderActions == null)
                return;

            if (player.RecorderActions.Count == 0)
            {
                player.Out.SendMessage("No actions recorded. Use /recorder start first.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            // make sure the name is unique for this player
            if (player.RecorderDbEntries?.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) == true)
            {
                player.Out.SendMessage($"A recorder named '{name}' already exists. Choose a different name.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            // Check per-player recorder limit
            if (Properties.RECORDER_MAX_PER_PLAYER > 0 && player.RecorderDbEntries?.Count >= Properties.RECORDER_MAX_PER_PLAYER)
            {
                player.Out.SendMessage($"You have reached the maximum number of recorders ({Properties.RECORDER_MAX_PER_PLAYER}). Delete one before saving a new one.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                player.RecorderActions = null;
                return;
            }

            // Auto-select the icon from the first recorded spell so the button looks
            // recognisable without the player having to run /recorder icon manually.
            int autoIconId = RecorderBaseIcon;
            var firstAction = player.RecorderActions.FirstOrDefault(a => a.Type == RecorderActionType.Spell);
            if (firstAction != null)
            {
                Spell s = SkillBase.GetSpellByID(firstAction.ID);
                if (s != null)
                    autoIconId = s.Icon;
            }

            DBCharacterRecorder dbEntry = new()
            {
                CharacterID = player.InternalID,
                Name = name,
                IconID = autoIconId,
                ActionsJson = JsonConvert.SerializeObject(player.RecorderActions)
            };

            GameServer.Database.AddObject(dbEntry);
            player.RecorderActions = null;
            player.RecorderDbEntries?.Add(dbEntry);

            // Update Players list
            RefreshPlayerRecorders(player);
            player.Out.SendMessage($"Recorder '{name}' saved.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        /// <summary>
        /// Deletes a recorder entry belonging to the specified player.
        /// </summary>
        /// <param name="player">Owning player.</param>
        /// <param name="name">Name of the recorder to remove.</param>
        /// <returns><c>true</c> if an entry was removed; <c>false</c> otherwise.</returns>
        public static bool DeleteRecording(GamePlayer player, string name)
        {
            if (player == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                    return false;

                GameServer.Database.DeleteObject(entry);
                player.RecorderDbEntries?.Remove(entry);
                RefreshPlayerRecorders(player);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] error deleting '{name}' for {player.Name}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Renames an existing recorder, ensuring the new name does not clash.
        /// </summary>
        public static bool RenameRecording(GamePlayer player, string oldName, string newName)
        {
            if (player == null || string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                return false;

            if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                return false; // nothing to do

            try
            {
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                    return false;

                if (player.RecorderDbEntries?.Any(e => e.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)) == true)
                    return false; // target already exists

                entry.Name = newName;
                // mark dirty so the DB framework flushes the change on SaveObject
                entry.Dirty = true;
                GameServer.Database.SaveObject(entry);
                RefreshPlayerRecorders(player);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] error renaming '{oldName}' to '{newName}' for {player.Name}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Removes a single action from a saved recorder by its 1-based position.
        /// Sends feedback messages to the player directly.
        /// </summary>
        /// <param name="player">Owning player.</param>
        /// <param name="name">Recorder name.</param>
        /// <param name="index">1-based position of the action to remove.</param>
        /// <returns><c>true</c> if the action was removed; <c>false</c> otherwise.</returns>
        public static bool DiscardAction(GamePlayer player, string name, int index)
        {
            if (player == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    player.Out.SendMessage($"Unknown recorder '{name}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return false;
                }

                List<RecorderAction> actions;
                try
                {
                    actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson) ?? new List<RecorderAction>();
                }
                catch (Exception ex)
                {
                    log.Error($"[RECORDER] error parsing actions for '{name}': {ex}");
                    player.Out.SendMessage($"[{name}] Action data is corrupt. Contact a GM.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return false;
                }

                int zeroIndex = index - 1;
                if (zeroIndex < 0 || zeroIndex >= actions.Count)
                {
                    player.Out.SendMessage($"Invalid index. {name} has {actions.Count} action(s). Valid positions are 1 to {actions.Count}.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return false;
                }

                RecorderAction removed = actions[zeroIndex];
                actions.RemoveAt(zeroIndex);

                if (actions.Count == 0)
                {
                    GameServer.Database.DeleteObject(entry);
                    player.RecorderDbEntries?.Remove(entry);
                    RefreshPlayerRecorders(player);
                    string removedDesc = GetActionDescription(removed, player.CharacterClass.ID);
                    player.Out.SendMessage($"[{name}] Action {index} ({removedDesc}) removed. Recorder is now empty and has been deleted.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return true;
                }

                entry.ActionsJson = JsonConvert.SerializeObject(actions);
                entry.Dirty = true;
                GameServer.Database.SaveObject(entry);
                RefreshPlayerRecorders(player);

                string desc = GetActionDescription(removed, player.CharacterClass.ID);
                player.Out.SendMessage($"[{name}] Action {index} ({desc}) removed.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] error discarding action from '{name}' for {player.Name}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Change the icon for a saved recorder. If iconId is null the next spell cast
        /// while recording will provide the icon.
        /// </summary>
        public static bool SetRecorderIcon(GamePlayer player, string name)
        {
            if (player == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                var entry = player.RecorderDbEntries?.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                    return false;

                // Queue pending change — icon is taken from the next spell the player casts.
                player.PendingRecorderIconName = name;
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] error setting icon for '{name}' on {player.Name}: {ex}");
                return false;
            }
        }
        #endregion

        /// <summary>
        /// Aborts the current in‑memory recording for a player without saving.
        /// </summary>
        /// <param name="player">Owning player.</param>
        /// <returns><c>true</c> if a recording was active and removed; otherwise <c>false</c>.</returns>
        public static bool CancelRecording(GamePlayer player)
        {
            if (player == null) return false;

            if (player.RecorderActions != null || HasPendingInsert(player) || HasPendingIcon(player))
            {
                player.RecorderActions = null;
                player.PendingInsertRecorderName = null;
                player.PendingInsertRecorderIndex = 0;
                player.PendingRecorderIconName = null;
                player.Out.SendMessage("Recording cancelled.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return true;
            }

            return false;
        }


        public static void ImportRecorderAsync(GamePlayer targetPlayer, string sourceCharName, string sourceRecorderName)
        {
            if (targetPlayer?.Client == null || string.IsNullOrEmpty(sourceCharName) || string.IsNullOrEmpty(sourceRecorderName))
                return;

            string playerId = targetPlayer.InternalID;

            // Check throttling: prevent spam imports from same player
            if (_lastImportTime.TryGetValue(playerId, out var lastTime))
            {
                if ((DateTime.UtcNow - lastTime).TotalMilliseconds < ImportThrottleMilliseconds)
                {
                    targetPlayer.Out.SendMessage("Import is on cooldown. Please wait a moment before trying again.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
            }

            // Update throttle timestamp
            _lastImportTime.AddOrUpdate(playerId, DateTime.UtcNow, (_, __) => DateTime.UtcNow);

            try
            {
                // Import uses only in-memory data — no database queries allowed on the game loop.
                // All account character names and recorder data are already loaded at login.

                // 1. Find source character by name — use cached character names
                string sourceCharId = null;
                if (targetPlayer.AccountCharacterNames != null)
                {
                    var kvp = targetPlayer.AccountCharacterNames.FirstOrDefault(
                        c => c.Value.Equals(sourceCharName, StringComparison.OrdinalIgnoreCase));
                    sourceCharId = kvp.Value != null ? kvp.Key : null;
                }

                if (sourceCharId == null)
                {
                    targetPlayer.Out.SendMessage($"Character '{sourceCharName}' not found on your account.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                // 2. Find the recorder scoped to that specific source character — use cached recorders
                if (!targetPlayer.AccountRecordersByCharId.TryGetValue(sourceCharId, out var sourceCharRecorders) || sourceCharRecorders == null)
                {
                    targetPlayer.Out.SendMessage($"'{sourceCharName}' has no recorders.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                var sourceRecorder = sourceCharRecorders.FirstOrDefault(r => r.Name.Equals(sourceRecorderName, StringComparison.OrdinalIgnoreCase));
                if (sourceRecorder == null)
                {
                    targetPlayer.Out.SendMessage($"'{sourceCharName}' has no recorder named '{sourceRecorderName}'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                // 3. Check for name collision on target — use cached target recorders
                if (targetPlayer.RecorderDbEntries != null && 
                    targetPlayer.RecorderDbEntries.Any(e => e.Name.Equals(sourceRecorder.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    targetPlayer.Out.SendMessage($"A recorder named '{sourceRecorder.Name}' already exists. Rename or delete it first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                // 3b. Check per-player recorder limit
                if (Properties.RECORDER_MAX_PER_PLAYER > 0 && targetPlayer.RecorderDbEntries?.Count >= Properties.RECORDER_MAX_PER_PLAYER)
                {
                    targetPlayer.Out.SendMessage($"You have reached the maximum number of recorders ({Properties.RECORDER_MAX_PER_PLAYER}). Delete one before importing a new one.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                // 4. Copy and insert
                var newRecorder = new DBCharacterRecorder
                {
                    CharacterID = targetPlayer.InternalID,
                    Name = sourceRecorder.Name,
                    IconID = sourceRecorder.IconID,
                    ActionsJson = sourceRecorder.ActionsJson
                };
                GameServer.Database.AddObject(newRecorder);
                RefreshPlayerRecorders(targetPlayer);

                targetPlayer.Out.SendMessage($"Recorder '{sourceRecorder.Name}' imported from {sourceCharName}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] Error importing recorder for player {targetPlayer.Name}: {ex.Message}", ex);
                targetPlayer.Out.SendMessage("An error occurred during import. Please try again.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        /// <summary>
        /// Lists all recorders from all characters on the player's account.
        /// Uses the in-memory <see cref="GamePlayer.AccountRecordersByCharId"/> and 
        /// <see cref="GamePlayer.AccountCharacterNames"/> caches loaded at login.
        /// </summary>
        public static void ListAccountRecorders(GamePlayer player)
        {
            if (player?.Client?.Account == null)
                return;

            try
            {
                // All account recorders are already loaded in RAM at login via AccountRecordersByCharId.
                // No DB query needed — just display what's cached.
                if (player.AccountRecordersByCharId == null || player.AccountRecordersByCharId.Count == 0)
                {
                    player.Out.SendMessage("No recorders found on any of your characters.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                var lines = new List<string>();
                bool hasAnyRecorders = false;

                // Iterate through cached recorders and display with character names from cache.
                foreach (var kvp in player.AccountRecordersByCharId)
                {
                    var charId = kvp.Key;
                    var recorders = kvp.Value;
                    
                    if (recorders == null || recorders.Count == 0)
                        continue;

                    hasAnyRecorders = true;

                    // Look up character name from cache; fall back to ID if not found.
                    string charName = player.AccountCharacterNames != null && player.AccountCharacterNames.TryGetValue(charId, out var name)
                        ? name
                        : $"Character {charId}";

                    lines.Add($"{charName}:");

                    foreach (var entry in recorders.OrderBy(e => e.Name))
                    {
                        lines.Add(entry.Name);

                        try
                        {
                            var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson);
                            if (actions != null && actions.Count > 0)
                            {
                                for (int i = 0; i < Math.Min(actions.Count, ListPreviewActionCount); i++)
                                    // Note: classId is not available from cached data; pass null for generic descriptions
                                    lines.Add($"- {GetActionDescription(actions[i], null)}");

                                if (actions.Count > ListPreviewActionCount)
                                    lines.Add($"... and {actions.Count - ListPreviewActionCount} more actions");

                                lines.Add("");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Warn($"[RECORDER] could not parse actions for '{entry.Name}': {ex.Message}");
                        }
                    }

                    lines.Add("");
                }

                if (!hasAnyRecorders)
                {
                    player.Out.SendMessage("No recorders found on any of your characters.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                player.Out.SendCustomTextWindow("Your Recorders", lines);
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] Error listing recorders for {player.Name}: {ex.Message}", ex);
                player.Out.SendMessage("An error occurred while listing recorders.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        private static string GetActionDescription(RecorderAction action, int? classId = null)
        {
            return action.Type switch
            {
                RecorderActionType.Spell =>
                    SkillBase.GetSpellByID(action.ID) is { } spell
                        ? $"Spell: {spell.Name}"
                        : $"Spell (ID: {action.ID})",

                RecorderActionType.Style =>
                    classId.HasValue && SkillBase.GetStyleByID(action.ID, classId.Value) is { } style
                        ? $"Style: {style.Name}"
                        : $"Style (ID: {action.ID})",

                RecorderActionType.Ability =>
                    (SkillBase.GetAbility(action.ID) ?? SkillBase.GetAbilityByInternalID(action.ID)) is { } abil
                        ? $"Ability: {abil.Name}"
                        : $"Ability (ID: {action.ID})",

                RecorderActionType.Command =>
                    action.Value is { Length: > 0 } cmd
                        ? $"Command: {(cmd[0] == '&' ? "/" + cmd[1..] : cmd)}"
                        : "Command",

                RecorderActionType.WeaponSwitch =>
                    !string.IsNullOrEmpty(action.Value)
                        ? $"Use: {action.Value}"
                        : $"Use (slot: {action.ID})",

                RecorderActionType.ItemCharge =>
                    !string.IsNullOrEmpty(action.Value) && action.Value.IndexOf(':') is int sep && sep >= 0
                        ? $"Item Use: {action.Value[(sep + 1)..]}"
                        : $"Item Use (slot: {action.ID})",

                _ => "Unknown action",
            };
        }
    }


    /// <summary>
    /// Categorises a single step in a recorded macro sequence.
    /// The enum is serialised as its string name so existing database rows remain compatible.
    /// </summary>
    public enum RecorderActionType
    {
        Unknown = 0,
        Spell,
        Style,
        Ability,
        Command,
        ItemCharge,
        WeaponSwitch,
    }

    [Serializable]
    public class RecorderAction
    {
        /// <summary>
        /// The kind of action. Stored as a string in JSON for readability and DB forward-compatibility.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public RecorderActionType Type { get; set; }

        /// <summary>Skill / spell / style / ability ID, or 0 for commands.</summary>
        public int ID { get; set; }

        /// <summary>
        /// Auxiliary text payload. Meaning varies by <see cref="Type"/>:
        /// <list type="bullet">
        ///   <item><term>Command</term><description>The raw command string (e.g. "&amp;assist").</description></item>
        ///   <item><term>ItemCharge</term><description>"{chargeType}:{spellName}" — e.g. "1:Minor Heal". Falls back to item display name if spell cannot be resolved.</description></item>
        ///   <item><term>WeaponSwitch</term><description>The display name of the weapon being switched to.</description></item>
        /// </list>
        /// </summary>
        public string Value { get; set; }
    }
}