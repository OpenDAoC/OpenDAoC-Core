using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    // This class manages the recording, storage, and execution of player‑defined macros (called "recorders").
    // It provides methods to start/stop recordings, save them to the database, and refresh
    // It's used to record multiple spells/abilities/styles/commands in sequence, and replay them via a single macro button.
    public class RecorderMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public const string RecorderLineKey = "Recorder";
        public const string RecorderDisplayName = "Recorder"; // Name which is displayed ingame
        public const int RecorderBaseIcon = 11130;      // Animist DD Icon as default

        // Base offset added to SpellID so recorder macro IDs never clash with real spell IDs.
        private const int SpellIdBase = 100_000;

        // Maximum number of actions shown inline when listing recorders via /recorder list.
        private const int ListPreviewActionCount = 3;

        // Cached string form of the spell type — avoids a ToString() allocation per entry build.
        private static readonly string RecorderSpellTypeString = eSpellType.RecorderAction.ToString();

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
        /// Re-fetches this player's recorder DB entries synchronously and rebuilds the
        /// spellbook. Called mid-session whenever a recorder is added, removed, or modified
        /// via a player command. Uses a targeted <see cref="DOLDB{T}.SelectObjects"/> query
        /// so only the owning player's rows are touched.
        /// For the initial character login load, DB access is avoided entirely: the rows are
        /// fetched asynchronously in <c>GamePlayer.LoadFromDatabaseAsync</c> and handed
        /// directly to <see cref="BuildPlayerRecorders"/>.
        /// </summary>
        public static void RefreshPlayerRecorders(GamePlayer player)
        {
            if (player?.Client == null) return;

            var dbEntries = DOLDB<DBCharacterRecorder>.SelectObjects(
                DB.Column("CharacterID").IsEqualTo(player.InternalID));
            BuildPlayerRecorders(player, dbEntries);
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
                                tooltipBuilder.AppendLine(GetActionDescription(action, player.CharacterClass.ID));
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
                    int tooltipId = player.Client.LastMacroToolTipID++;

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

        // This gets called from /recorder start command
        public static void StartRecording(GamePlayer player)
        {
            if (player == null) return;
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

            var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                           .And(DB.Column("Name").IsEqualTo(name));
            var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);
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

            var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                           .And(DB.Column("Name").IsEqualTo(name));
            var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);
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
                var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                               .And(DB.Column("Name").IsEqualTo(name));
                var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);
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
                var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                               .And(DB.Column("Name").IsEqualTo(recorderName));
                var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);
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
            }

            // Disable recording recorders
            if (spell is RecorderSpell)
                return;

            var action = new RecorderAction { Type = RecorderActionType.Spell, ID = spell.ID };

            if (TryHandlePendingInsert(player, action))
                return;

            if (!IsPlayerRecording(player))
                return;

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
            var whereCheck = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                             .And(DB.Column("Name").IsEqualTo(name));
            var existing = GameServer.Database.SelectObject<DBCharacterRecorder>(whereCheck);
            if (existing != null)
            {
                player.Out.SendMessage($"A recorder named '{name}' already exists. Choose a different name.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
                var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                               .And(DB.Column("Name").IsEqualTo(name));
                var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);

                if (entry == null)
                    return false;

                GameServer.Database.DeleteObject(entry);
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
                var whereOld = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                                 .And(DB.Column("Name").IsEqualTo(oldName));
                var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(whereOld);
                if (entry == null)
                    return false;

                var whereNew = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                                 .And(DB.Column("Name").IsEqualTo(newName));
                if (GameServer.Database.SelectObject<DBCharacterRecorder>(whereNew) != null)
                    return false; // target already exists

                entry.Name = newName;
                // mark object dirty so SaveObject updates it (SelectObject may return non-dirty)
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
                var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                               .And(DB.Column("Name").IsEqualTo(name));
                var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);

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
        public static bool SetRecorderIcon(GamePlayer player, string name, int? iconId)
        {
            if (player == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                var where = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                               .And(DB.Column("Name").IsEqualTo(name));
                var entry = GameServer.Database.SelectObject<DBCharacterRecorder>(where);
                if (entry == null)
                    return false;

                if (iconId.HasValue)
                {
                    entry.IconID = iconId.Value;
                    entry.Dirty = true;
                    GameServer.Database.SaveObject(entry);
                    RefreshPlayerRecorders(player);
                    return true;
                }

                // queue pending change
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


        // This needs testing
        // Currently we only allow import from same account
        public static void ImportRecorder(GamePlayer targetPlayer, string sourceCharName, string sourceRecorderName)
        {
            if (targetPlayer == null || string.IsNullOrEmpty(sourceCharName) || string.IsNullOrEmpty(sourceRecorderName))
                return;

            // 1. Find the source character by name first — avoids the previous bug where
            //    a Name-only lookup could match a recorder from a different character.
            var sourceChar = GameServer.Database.SelectObject<DbCoreCharacter>(
                DB.Column("Name").IsEqualTo(sourceCharName));
            if (sourceChar == null)
            {
                targetPlayer.Out.SendMessage($"Character '{sourceCharName}' not found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 2. Check same account before touching any recorder data
            if (!string.Equals(sourceChar.AccountName, targetPlayer.Client.Account.Name, StringComparison.OrdinalIgnoreCase))
            {
                targetPlayer.Out.SendMessage("Import failed. You can only import from your own account.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 3. Find the recorder scoped to that specific character
            var sourceRecorder = GameServer.Database.SelectObject<DBCharacterRecorder>(
                DB.Column("CharacterID").IsEqualTo(sourceChar.ObjectId)
                .And(DB.Column("Name").IsEqualTo(sourceRecorderName)));
            if (sourceRecorder == null)
            {
                targetPlayer.Out.SendMessage($"'{sourceCharName}' has no recorder named '{sourceRecorderName}'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 4. Check for name collision on target
            var existing = GameServer.Database.SelectObject<DBCharacterRecorder>(
                DB.Column("CharacterID").IsEqualTo(targetPlayer.InternalID)
                .And(DB.Column("Name").IsEqualTo(sourceRecorder.Name))
            );
            if (existing != null)
            {
                targetPlayer.Out.SendMessage($"A recorder named '{sourceRecorder.Name}' already exists. Rename or delete it first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 5. Copy and insert
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

        /// <summary>
        /// Lists all recorders from all characters on the player's account.
        /// </summary>
        public static void ListAccountRecorders(GamePlayer player)
        {
            if (player?.Client?.Account == null)
                return;

            try
            {
                // Get all characters for this account
                var chars = GameServer.Database.SelectObjects<DbCoreCharacter>(
                    DB.Column("AccountName").IsEqualTo(player.Client.Account.Name))
                    .OrderBy(c => c.Name)
                    .ToList();

                if (chars.Count == 0)
                {
                    player.Out.SendMessage("No characters found on your account.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                var lines = new List<string>();
                bool hasAnyRecorders = false;

                // Fetch all chars for this account, then all their recorder rows in one
                // batched query using IsIn rather than N individual selects.
                var charIds = chars.Select(c => c.ObjectId).ToList();
                var allRecorders = GameServer.Database.SelectObjects<DBCharacterRecorder>(
                    DB.Column("CharacterID").IsIn(charIds))
                    .GroupBy(r => r.CharacterID)
                    .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Name).ToList());

                foreach (var character in chars)
                {
                    if (!allRecorders.TryGetValue(character.ObjectId, out var recorders) || recorders.Count == 0)
                        continue;

                    hasAnyRecorders = true;
                    string className = ((eCharacterClass)character.Class).ToString();
                    lines.Add($"{character.Name}  ({className}):");

                    foreach (var recorder in recorders)
                    {
                        lines.Add($"{recorder.Name}");

                        // Show the actions inside this recorder
                        try
                        {
                            var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(recorder.ActionsJson);
                            if (actions != null && actions.Count > 0)
                            {
                                for (int i = 0; i < Math.Min(actions.Count, ListPreviewActionCount); i++)
                                {
                                    var action = actions[i];
                                    string actionDesc = GetActionDescription(action, character.Class);
                                    lines.Add($"- {actionDesc}");
                                }
                                if (actions.Count > ListPreviewActionCount)
                                    lines.Add($"... and {actions.Count - ListPreviewActionCount} more actions");

                                lines.Add(""); // empty line after each recorder
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Warn($"[RECORDER] could not parse actions for '{recorder.Name}' on '{character.Name}': {ex.Message}");
                        }
                    }
                    lines.Add(""); // empty line after each character
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
                log.Error($"[RECORDER] error listing recorders: {ex}");
                player.Out.SendMessage("Error retrieving recorders.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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