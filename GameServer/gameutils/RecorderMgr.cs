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
    public class RecorderMgr
    {
        private static Dictionary<GamePlayer, List<RecorderAction>> _activeRecordings = new Dictionary<GamePlayer, List<RecorderAction>>();

        public const string RecorderLineKey = "Recorder";
        public const string RecorderDisplayName = "Recorder";
        public const int RecorderBaseIcon = 700;
        public const string TempPropKey = "RecorderSpells"; 

        #region Initialization
        public static bool Init()
        {
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerLogin);
            Console.WriteLine("[RECORDER] System erfolgreich initialisiert.");
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
        public static void RefreshPlayerRecorders(GamePlayer player)
        {
            if (player == null) return;

            try
            {
                var dbEntries = GameServer.Database.SelectAllObjects<DBCharacterRecorder>()
                    .Where(r => r.CharacterID == player.InternalID)
                    .OrderBy(r => r.LastTimeRowUpdated)
                    .ToList();

                List<Spell> playerSpells = new List<Spell>();
                int idCounter = 0;

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
                                    if (s != null) dynamicDescription += $"Spell: {s.Name}\n";
                                }
                                else if (action.Type == "Command")
                                {
                                    dynamicDescription += $"Cmd: {action.ID}\n";
                                }
                            }
                        }
                    }
                    catch { dynamicDescription = "Recorded Macro (Data Error)"; }

                    DbSpell db = new DbSpell
                    {
                        Name = entry.Name,
                        Icon = (ushort)entry.IconID,
                        ClientEffect = (ushort)entry.IconID,
                        SpellID = 110000 + idCounter,
                        Target = "Self",
                        CastTime = 0,
                        Type = "RecorderAction", 
                        Description = dynamicDescription
                    };

                    playerSpells.Add(new Spell(db, 1));
                    idCounter++;
                }

                // KORREKTUR: SetProperty mit großem 'S'
                player.TempProperties.SetProperty(TempPropKey, playerSpells);

                player.RemoveSpecialization(RecorderLineKey);
                if (playerSpells.Count > 0)
                {
                    player.AddSpecialization(new RecorderSpecialization(RecorderLineKey, RecorderDisplayName, RecorderBaseIcon));
                }

                if (player.Client != null)
                {
                    player.Out.SendUpdatePlayerSkills(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECORDER] Error in RefreshPlayerRecorders: {ex.Message}");
            }
        }
        #endregion

        #region Recording Actions
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
            player.Out.SendMessage("Aufnahme gestartet...", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        public static void RecordAction(GamePlayer player, Spell spell)
        {
            if (player == null || spell == null || !IsPlayerRecording(player)) return;
            if (spell.Name.Contains(RecorderDisplayName) || spell.SpellType.ToString() == "RecorderAction") return;

            lock (_activeRecordings)
            {
                if (_activeRecordings.ContainsKey(player))
                {
                    _activeRecordings[player].Add(new RecorderAction { Type = "Spell", ID = spell.ID });
                    player.Out.SendMessage($"[REC] {spell.Name} hinzugefügt.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
            }
        }

        public static void StopAndSaveRecording(GamePlayer player, string name)
        {
            lock (_activeRecordings)
            {
                if (!_activeRecordings.ContainsKey(player)) return;
                var actions = _activeRecordings[player];

                if (actions.Count == 0)
                {
                    _activeRecordings.Remove(player);
                    return;
                }

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
            }
        }
        #endregion

        #region Helper Classes
        public class RecorderSpecialization : Specialization
        {
            private SpellLine m_line;

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
                    // KORREKTUR: GetProperty mit großem 'G'
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

    [Serializable]
    public class RecorderAction
    {
        public string Type { get; set; }
        public int ID { get; set; }
        public string Value { get; set; }
    }
}