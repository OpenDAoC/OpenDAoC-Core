using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.GS.Styles;
using Newtonsoft.Json;
using DOL.Logging;

namespace DOL.GS
{
    public class RecorderMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Dictionary<GamePlayer, List<RecorderAction>> _activeRecordings = new();
        // track pending icon assignments when next spell is cast
        private static readonly Dictionary<GamePlayer, string> _pendingIconChange = new();
        public const string RecorderLineKey = "Recorder";
        public const string RecorderDisplayName = "Recorder"; // Name which is displayed ingame
        public const int RecorderBaseIcon = 11130; // Animist DD Icon as default

        #region Initialization
        public static bool Init()
        {
            if (!Properties.RECORDER_ENABLED)
            {
                log.Info("[RECORDER] System disabled via server property 'recorder_enabled'.");
                return false;
            }
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerLogin);
            log.Debug("[RECORDER] System fully initialized.");
            return true;
        }

        // This gets called, when player logs in
        private static void OnPlayerLogin(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is GamePlayer player) RefreshPlayerRecorders(player);
        }
        #endregion

        #region Core Logic
        // Refresh player's spellbook, adding recorders
        // This gets called on login & when any changes are made to recorders
        public static void RefreshPlayerRecorders(GamePlayer player)
        {
            if (player?.Client == null) return;

            try
            {
                var dbEntries = GameServer.Database.SelectAllObjects<DBCharacterRecorder>()
                    .Where(r => r.CharacterID == player.InternalID)
                    .OrderBy(r => r.ID)
                    .ToList();

                player.SpellMacros.Clear();

                foreach (var entry in dbEntries)
                {
                    // Design the tooltip
                    string dynamictooltip = "[Recorder]\n";
                    try
                    {
                        var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(entry.ActionsJson);
                        if (actions != null)
                        {
                            foreach (var action in actions)
                            {
                                if (action.Type == "Spell")
                                {
                                    Spell s = SkillBase.GetSpellByID(action.ID);
                                    if (s != null) dynamictooltip += $"Spell: {s.Name}\n";
                                }
                                else if (action.Type == "Style")
                                {
                                    Style style = SkillBase.GetStyleByID(action.ID, player.CharacterClass.ID);
                                    if (style != null) dynamictooltip += $"Style: {style.Name}\n";
                                }
                                else if (action.Type == "Ability")
                                {
                                    // normally the action ID is the ability's external ID
                                    Ability abil = SkillBase.GetAbility(action.ID) ?? SkillBase.GetAbilityByInternalID(action.ID);
                                    if (abil != null) dynamictooltip += $"Ability: {abil.Name}\n";
                                }
                                else if (action.Type == "Command")
                                {
                                    // command text is stored in Value field; ensure it begins with '/'
                                    string cmd = action.Value ?? string.Empty;
                                    if (cmd.Length > 0 && cmd[0] == '&')
                                        cmd = "/" + cmd[1..];
                                    dynamictooltip += $"Cmd: {cmd}\n";
                                }
                            }
                        }
                    }
                    catch (Exception ex) 
                    { 
                        dynamictooltip = "Recorded Macro (Data Error)";
                        log.Error($"[RECORDER] Error parsing actions for {entry.Name}: {ex}");
                    }

                    int uniqueID = 100000 + player.SpellMacros.Count;
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
                        Type = eSpellType.RecorderAction.ToString(),
                        Description = dynamictooltip,
                        TooltipId = (ushort)tooltipId,
                        Power = 0,
                    };

                    player.SpellMacros.Add(new RecorderSpell(db, uniqueLevel, entry));
                }

                // Cleanup & refresh later on
                player.RemoveSpecialization(RecorderLineKey);

                if (player.SpellMacros.Count > 0)
                {
                    // Add Spec to player
                    player.AddSpecialization(new RecorderSpecialization(RecorderLineKey, RecorderDisplayName, 1));
                }

                // Update player
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
            public RecorderSpell(DbSpell db, int level, DBCharacterRecorder data) : base(db, level) => RecordData = data;
        }

        public class RecorderSpecialization : Specialization
        {
            private SpellLine m_line = null;

            public RecorderSpecialization(string keyname, string displayname, ushort icon) : base(keyname, displayname, icon)
            {
                m_line = new SpellLine(RecorderLineKey, RecorderLineKey, RecorderLineKey, false);
            }

            public override bool Trainable => false;

            public override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living)
            {
                Dictionary<SpellLine, List<Skill>> dict = new();
                if (living is GamePlayer player)
                {
                    if (player.SpellMacros != null && player.SpellMacros.Count > 0)
                    {
                        List<Skill> skills = player.SpellMacros.Select(s => s as Skill).ToList();
                        dict.Add(m_line, skills);
                    }
                }
                return dict;
            }

            public override List<SpellLine> GetSpellLinesForLiving(GameLiving living)
            {
                return new List<SpellLine>() { m_line };
            }
        }
        #endregion

        #region Recording Logic
        public static bool IsPlayerRecording(GamePlayer player)
        {
            lock (_activeRecordings) return _activeRecordings.ContainsKey(player);
        }
        public static bool HasPendingIcon(GamePlayer player)
        {
            lock (_pendingIconChange) return _pendingIconChange.ContainsKey(player);
        }

        // This gets called from /recorder start command
        public static void StartRecording(GamePlayer player)
        {
            lock (_activeRecordings) _activeRecordings[player] = new List<RecorderAction>();
            player.Out.SendMessage("Recording started.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        // This gets called from CastingComponent/Style..../Ability...
        public static void RecordAction(GamePlayer player, Spell spell)
        {
            // Optionally handle pending icon change before recording action
            if (player != null && spell != null)
            {
                lock (_pendingIconChange)
                {
                    if (_pendingIconChange.TryGetValue(player, out var recorderName))
                    {
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
                            player.Out.SendMessage($"Recorder '{recorderName}' icon set from next spell ({spell.Name}).", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        }
                        _pendingIconChange.Remove(player);
                    }
                }
            }

            // Disable recording recorders
            if (spell is RecorderSpell || !IsPlayerRecording(player))
                return;

            // Record spells/styles/...
            lock (_activeRecordings)
            {
                if (_activeRecordings.TryGetValue(player, out var actions))
                {
                    actions.Add(new RecorderAction { Type = "Spell", ID = spell.ID });
                    int pos = actions.Count; // new action position
                    player.Out.SendMessage($"{pos}. Spell '{spell.Name}' added", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
            }
        }

        public static void RecordAction(GamePlayer player, Style style)
        {
            if (style == null || !IsPlayerRecording(player))
                return;

            lock (_activeRecordings)
            {
                if (_activeRecordings.TryGetValue(player, out var actions))
                {
                    actions.Add(new RecorderAction { Type = "Style", ID = style.ID });
                    int pos = actions.Count;
                    player.Out.SendMessage($"{pos}. Style '{style.Name}' added", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
            }
        }

        public static void RecordAction(GamePlayer player, Ability ability)
        {
            if (ability == null || !IsPlayerRecording(player)) return;

            lock (_activeRecordings)
            {
                if (_activeRecordings.TryGetValue(player, out var actions))
                {
                    actions.Add(new RecorderAction { Type = "Ability", ID = ability.ID });
                    int pos = actions.Count;
                    player.Out.SendMessage($"{pos}. Ability '{ability.Name}' added", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
            }
        }

        /// <summary>
        /// Record a player‑entered command while recording is active.
        /// </summary>
        public static void RecordAction(GamePlayer player, string command)
        {
            if (string.IsNullOrEmpty(command) || !IsPlayerRecording(player))
                return;

            lock (_activeRecordings)
            {
                if (_activeRecordings.TryGetValue(player, out var actions))
                {
                    actions.Add(new RecorderAction { Type = "Command", ID = 0, Value = command });
                    int pos = actions.Count;
                    player.Out.SendMessage($"{pos}. Command '{command}' added", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
            }
        }

        public static void StopAndSaveRecording(GamePlayer player, string name)
        {
            lock (_activeRecordings)
            {
                if (!_activeRecordings.TryGetValue(player, out var actions) || actions.Count == 0) return;

                // make sure the name is unique for this player
                var whereCheck = DB.Column("CharacterID").IsEqualTo(player.InternalID)
                                 .And(DB.Column("Name").IsEqualTo(name));
                var existing = GameServer.Database.SelectObject<DBCharacterRecorder>(whereCheck);
                if (existing != null)
                {
                    player.Out.SendMessage($"A recorder named '{name}' already exists.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return;
                }

                // We set first spell icon as recorder icon
                int autoIconId = RecorderBaseIcon;
                // Why we do this ?
                var firstAction = actions.FirstOrDefault(a => a.Type == "Spell");
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
                    ActionsJson = JsonConvert.SerializeObject(actions)
                };

                GameServer.Database.AddObject(dbEntry);
                _activeRecordings.Remove(player);

                // Update Players list
                RefreshPlayerRecorders(player);
                player.Out.SendMessage($"Recorder '{name}' saved.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
            }
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
        /// Change the icon for a saved recorder. If iconId is null the next spell cast
        /// while recording will provide the icon.
        /// </summary>
        public static bool SetRecorderIcon(GamePlayer player, string name, int? iconId)
        {
            if (player == null || string.IsNullOrEmpty(name))
                return false;

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
            lock (_pendingIconChange)
            {
                _pendingIconChange[player] = name;
            }
            return true;
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

            lock (_activeRecordings)
            {
                if (_activeRecordings.ContainsKey(player))
                {
                    _activeRecordings.Remove(player);
                    player.Out.SendMessage("Recording cancelled.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    return true;
                }
            }

            return false;
        }


        // This needs testing
        // Currently we only allow import from same account
        public static void ImportRecorder(GamePlayer targetPlayer, string sourceCharName, string sourceRecorderName)
        {
            // 1. Find recorder with matching name
            var sourceRecorder = GameServer.Database.SelectObject<DBCharacterRecorder>(
                DB.Column("Name").IsEqualTo(sourceRecorderName)
            );
            if (sourceRecorder == null)
            {
                targetPlayer.Out.SendMessage($"This player has no recorders.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 2. Get the source character
            var sourceChar = GameServer.Database.FindObjectByKey<DbCoreCharacter>(sourceRecorder.CharacterID);
            if (sourceChar == null)
            {
                targetPlayer.Out.SendMessage($"This player has no recorders.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 3. Check same account
            if (!string.Equals(sourceChar.AccountName, targetPlayer.Client.Account.Name, StringComparison.OrdinalIgnoreCase))
            {
                targetPlayer.Out.SendMessage("You can only import recorders from characters on your own account.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 4. Verify character name matches
            if (!string.Equals(sourceChar.Name, sourceCharName, StringComparison.OrdinalIgnoreCase))
            {
                targetPlayer.Out.SendMessage($"Recorder '{sourceRecorderName}' does not belong to character '{sourceCharName}'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 5. Check for name collision on target
            var existing = GameServer.Database.SelectObject<DBCharacterRecorder>(
                DB.Column("CharacterID").IsEqualTo(targetPlayer.InternalID)
                .And(DB.Column("Name").IsEqualTo(sourceRecorder.Name))
            );
            if (existing != null)
            {
                targetPlayer.Out.SendMessage($"You already have a recorder named '{sourceRecorder.Name}'. Please rename or delete it first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 6. Copy and insert
            var newRecorder = new DBCharacterRecorder
            {
                CharacterID = targetPlayer.InternalID,
                Name = sourceRecorder.Name,
                IconID = sourceRecorder.IconID,
                ActionsJson = sourceRecorder.ActionsJson
            };
            GameServer.Database.AddObject(newRecorder);
            RefreshPlayerRecorders(targetPlayer);

            targetPlayer.Out.SendMessage($"Successfully imported recorder '{sourceRecorder.Name}' from '{sourceCharName}'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                var chars = GameServer.Database.SelectAllObjects<DbCoreCharacter>()
                    .Where(c => c.AccountName == player.Client.Account.Name)
                    .OrderBy(c => c.Name)
                    .ToList();

                if (chars.Count == 0)
                {
                    player.Out.SendMessage("No characters found on your account.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                var lines = new List<string>();
                bool hasAnyRecorders = false;

                foreach (var character in chars)
                {
                    var recorders = GameServer.Database.SelectAllObjects<DBCharacterRecorder>()
                        .Where(r => r.CharacterID == character.ObjectId)
                        .OrderBy(r => r.Name)
                        .ToList();

                    if (recorders.Count > 0)
                    {
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
                                    for (int i = 0; i < Math.Min(actions.Count, 3); i++) // Show first 3 actions
                                    {
                                        var action = actions[i];
                                        string actionDesc = GetActionDescription(action, character.Class);
                                        lines.Add($"- {actionDesc}");
                                    }
                                    if (actions.Count > 3)
                                        lines.Add($"... and {actions.Count - 3} more actions");
                                    
                                    lines.Add(""); // empty line after each recorder
                                }
                            }
                            catch { }
                        }
                        lines.Add(""); // second empty line for next character
                    }
                }

                if (!hasAnyRecorders)
                {
                    player.Out.SendMessage("You have no recorders on any of your characters.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
            switch (action.Type)
            {
                case "Spell":
                    {
                        Spell spell = SkillBase.GetSpellByID(action.ID);
                        if (spell != null)
                            return $"Spell: {spell.Name}";
                        return $"Spell (ID: {action.ID})";
                    }
                case "Style":
                    {
                        // If we have a class ID, try to look up the style by name
                        if (classId.HasValue)
                        {
                            Style style = SkillBase.GetStyleByID(action.ID, classId.Value);
                            if (style != null)
                                return $"Style: {style.Name}";
                        }
                        return $"Style (ID: {action.ID})";
                    }
                case "Ability":
                    {
                        Ability ability = SkillBase.GetAbility(action.ID) ?? SkillBase.GetAbilityByInternalID(action.ID);
                        if (ability != null)
                            return $"Ability: {ability.Name}";
                        return $"Ability (ID: {action.ID})";
                    }
                case "Command":
                    return $"Command: {action.Value}";
                default:
                    return "Unknown action";
            }
        }
    }


    [Serializable]
    public class RecorderAction
    {
        public string Type { get; set; }
        public int ID { get; set; }
        public string Value { get; set; }
    }
}