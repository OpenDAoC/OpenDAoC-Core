using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using Newtonsoft.Json;

namespace DOL.GS
{
    /// <summary>
    /// Manager for recording and playback of player macros.
    /// Uses instance-based storage to ensure character isolation.
    /// </summary>
    public class RecorderMgr
    {
        // Tracks active recording sessions in memory
        private static readonly Dictionary<GamePlayer, List<RecorderAction>> _activeRecordings = new Dictionary<GamePlayer, List<RecorderAction>>();

        // Identification Constants
        public const string RecorderLineKey = "Recorder";
        public const string RecorderDisplayName = "Recorder";
        public const int RecorderBaseIcon = 700;
        public const string TempPropKey = "RecorderSpells";

        #region Initialization
        public static bool Init()
        {
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerLogin);
            
            Console.WriteLine("[RECORDER] System successfully initialized and registered.");
            return true;
        }

        private static void OnPlayerLogin(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is GamePlayer player)
            {
                RefreshPlayerRecorders(player);
            }
        }
        #endregion

        #region Core Logic
        /// <summary>
        /// Loads all macros from the database and assigns them to the player's spellbook.
        /// </summary>
        public static void RefreshPlayerRecorders(GamePlayer player)
        {
            if (player == null || player.Client == null) return;

            try
            {
                // 1. Fetch character-specific entries from Database
                var dbEntries = GameServer.Database.SelectAllObjects<DBCharacterRecorder>()
                    .Where(r => r.CharacterID == player.InternalID)
                    .OrderBy(r => r.LastTimeRowUpdated)
                    .ToList();

                List<Spell> playerSpells = new List<Spell>();

                foreach (var entry in dbEntries)
                {
                    string dynamicDescription = "Recorded Actions:\n";
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
                                    if (s != null) dynamicDescription += $"- Spell: {s.Name}\n";
                                }
                                else if (action.Type == "Command")
                                {
                                    dynamicDescription += $"- Cmd: {action.ID}\n";
                                }
                            }
                        }
                    }
                    catch { dynamicDescription = "Recorded Macro (Data Error)"; }

                    // 2. Generate unique Tooltip ID from Client session
                    int tooltipId = player.Client.LastMacroToolTipID++;

                    // 3. Create Spell Object (Macro-Optimized)
                    DbSpell db = new DbSpell
                    {
                        Name = entry.Name,
                        Icon = (ushort)entry.IconID,
                        ClientEffect = (ushort)entry.IconID,
                        SpellID = 110000 + tooltipId,
                        Target = "Self",
                        CastTime = 0,
                        Type = "RecorderAction", 
                        Description = dynamicDescription,
                        Power = 0,
                        TooltipId = (ushort)tooltipId
                    };

                    // Use MacroSpell if your core supports it for better tooltip handling, else use Spell
                    playerSpells.Add(new Spell(db, 1));
                }

                // 4. Isolation: Store data directly on the player object via TempProperties
                player.TempProperties.SetProperty(TempPropKey, playerSpells);

                // 5. Update Specialization (Spellbook View)
                player.RemoveSpecialization(RecorderLineKey);
                if (playerSpells.Count > 0)
                {
                    player.AddSpecialization(new RecorderSpecialization(RecorderLineKey, RecorderDisplayName, RecorderBaseIcon));
                }

                // 6. Synchronize with Client
                player.Out.SendUpdatePlayerSkills(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECORDER] Error in RefreshPlayerRecorders for {player.Name}: {ex.Message}");
            }
        }
        #endregion

        #region Recording Logic
        public static bool IsPlayerRecording(GamePlayer player)
        {
            if (player == null) return false;
            lock (_activeRecordings) { return _activeRecordings.ContainsKey(player); }
        }

        public static void StartRecording(GamePlayer player)
        {
            if (player == null) return;
            lock (_activeRecordings)
            {
                _activeRecordings[player] = new List<RecorderAction>();
            }
            player.Out.SendMessage("Recording started. Cast spells to add them to your macro.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
        }

        public static void RecordAction(GamePlayer player, Spell spell)
        {
            if (player == null || spell == null || !IsPlayerRecording(player)) return;

            // Prevent infinite recursion (recording the recorder)
            if (spell.Name.Contains(RecorderDisplayName) || spell.SpellType.ToString() == "RecorderAction") return;

            lock (_activeRecordings)
            {
                if (_activeRecordings.TryGetValue(player, out List<RecorderAction> actions))
                {
                    actions.Add(new RecorderAction { Type = "Spell", ID = spell.ID });
                    player.Out.SendMessage($"[REC] Added: {spell.Name}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
            }
        }

        public static void StopAndSaveRecording(GamePlayer player, string name)
        {
            lock (_activeRecordings)
            {
                if (!_activeRecordings.TryGetValue(player, out List<RecorderAction> actions)) return;

                if (actions.Count == 0)
                {
                    player.Out.SendMessage("Recording canceled: No actions recorded.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    _activeRecordings.Remove(player);
                    return;
                }

                // Auto-Icon Selection: Use the icon of the first recorded spell
                int autoIconId = RecorderBaseIcon;
                var firstAction = actions.FirstOrDefault(a => a.Type == "Spell");
                if (firstAction != null)
                {
                    Spell s = SkillBase.GetSpellByID(firstAction.ID);
                    if (s != null) autoIconId = s.Icon;
                }

                DBCharacterRecorder dbEntry = new DBCharacterRecorder
                {
                    CharacterID = player.InternalID,
                    Name = name,
                    IconID = autoIconId,
                    ActionsJson = JsonConvert.SerializeObject(actions)
                };

                GameServer.Database.AddObject(dbEntry);
                _activeRecordings.Remove(player);
                
                RefreshPlayerRecorders(player);
                player.Out.SendMessage($"Macro '{name}' saved successfully.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            }
        }
        #endregion

        #region Helper Classes
        /// <summary>
        /// Specialization class that maps stored spells to the player's skill window.
        /// </summary>
        public class RecorderSpecialization : Specialization
        {
            private readonly SpellLine m_line;

            public RecorderSpecialization(string keyname, string displayname, ushort icon) : base(keyname, displayname, icon)
            {
                m_line = new SpellLine("RecorderLine", displayname, "None", true);
            }

            public override bool Trainable => false;

            public override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living)
            {
                var dict = new Dictionary<SpellLine, List<Skill>>();
                if (living is GamePlayer player)
                {
                    var spells = player.TempProperties.GetProperty<List<Spell>>(RecorderMgr.TempPropKey);
                    if (spells != null)
                    {
                        dict.Add(m_line, spells.Cast<Skill>().ToList());
                    }
                }
                return dict;
            }

            public override List<SpellLine> GetSpellLinesForLiving(GameLiving living)
            {
                return new List<SpellLine> { m_line };
            }
        }
        #endregion
    }

    /// <summary>
    /// Model for a single macro action.
    /// </summary>
    [Serializable]
    public class RecorderAction
    {
        public string Type { get; set; }
        public int ID { get; set; }
        public string Value { get; set; }
    }
}