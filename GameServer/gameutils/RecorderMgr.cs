using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using Newtonsoft.Json;

namespace DOL.GS
{
    /// <summary>
    /// Manager für das Aufnehmen und Abspielen von Makros.
    /// Nutzt die Admin-Logik für dynamische Specializations und lokale SpellLines.
    /// </summary>
    public class RecorderMgr
    {
        // RAM-Speicher
        private static Dictionary<GamePlayer, List<RecorderAction>> _activeRecordings = new Dictionary<GamePlayer, List<RecorderAction>>();
        public static Dictionary<string, List<Spell>> _playerSpellCache = new Dictionary<string, List<Spell>>();

        // Konstanten für die Identifizierung
        public const string RecorderLineKey = "Recorder";
        public const string RecorderDisplayName = "Recorder";
        public const int RecorderBaseIcon = 700;

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
        /// <summary>
        /// Lädt alle Makros aus der Datenbank und weist dem Spieler die Spezialisierung zu.
        /// </summary>
        public static void RefreshPlayerRecorders(GamePlayer player)
        {
            if (player == null) return;

            try
            {
                // 1. Makros für diesen Charakter aus der Datenbank laden
                var dbEntries = GameServer.Database.SelectAllObjects<DBCharacterRecorder>()
                    .Where(r => r.CharacterID == player.InternalID)
                    .OrderBy(r => r.LastTimeRowUpdated) // Sortiert chronologisch aufsteigend
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
                                else if (action.Type == "Style")
                                {
                                    // Versuche den Style-Namen für den Tooltip aufzulösen
                                    //Style style = SkillBase.GetStyleByID(action.ID, player.CharacterClass.ID);
                                    //if (style != null)
                                    //    dynamicDescription += $"Style: {style.Name}\n";
                                    //else
                                    ///    dynamicDescription += $"Style: {action.ID}\n";
                                }
                                else if (action.Type == "Command")
                                {
                                    dynamicDescription += $"Cmd: {action.ID}\n";
                                }
                            }
                        }
                    }
                    catch { dynamicDescription = "Recorded Macro (Data Error)"; }

                    // 2. Temporäres Spell-Objekt für das Zauberbuch erstellen
                    DbSpell db = new DbSpell
                    {
                        Name = entry.Name,
                        Icon = (ushort)entry.IconID,
                        ClientEffect = (ushort)entry.IconID,
                        SpellID = 110000 + idCounter,
                        Target = "Self",
                        CastTime = 0,
                        // WICHTIG: Muss zum SpellHandler Attribut [SpellHandler(eSpellType.Macro)] passen
                        Type = eSpellType.RecorderAction.ToString(),
                        Description = dynamicDescription
                    };

                    playerSpells.Add(new Spell(db, 1));
                    idCounter++;
                }

                // 3. Den RAM-Cache füllen, damit der Handler weiß, was er tun soll
                lock (_playerSpellCache)
                {
                    _playerSpellCache[player.InternalID] = playerSpells;
                }

                // 4. Spezialisierung (Zauberlinie) verwalten
                player.RemoveSpecialization(RecorderLineKey);
                if (playerSpells.Count > 0)
                {
                    // Adds Recorder SpellLine to spellline window
                    player.AddSpecialization(new RecorderSpecialization(RecorderLineKey, RecorderDisplayName, RecorderBaseIcon));
                }

                // 5. Dem Client mitteilen, dass sich die Skills geändert haben
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
            player.Out.SendMessage("Aufnahme gestartet. Benutze Zauber, um sie dem Makro hinzuzufügen.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        public static void RecordAction(GamePlayer player, Spell spell)
        {
            if (player == null || spell == null || !IsPlayerRecording(player)) return;

            // Verhindert, dass Makros sich selbst aufnehmen
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
                    player.Out.SendMessage("Aufnahme abgebrochen: Keine Aktionen aufgezeichnet.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    _activeRecordings.Remove(player);
                    return;
                }

                // Erstes Icon als Standard für das Makro nutzen
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
                player.Out.SendMessage($"Makro '{name}' wurde erfolgreich gespeichert.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            }
        }
        #endregion

        #region Helper Classes
        /// <summary>
        /// Die Spezialisierung, die die Makros im Zauberbuch gruppiert.
        /// </summary>
        public class RecorderSpecialization : Specialization
        {
            private SpellLine m_line;

            public RecorderSpecialization(string keyname, string displayname, ushort icon) : base(keyname, displayname, icon)
            {
                // Erstellt eine lokale SpellLine, die nur für diese Spec existiert
                m_line = new SpellLine(keyname, displayname, "None", true);
            }

            public override bool Trainable => false;

            // Mappt die Cache-Spells auf die lokale SpellLine
            public override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living)
            {
                var dict = new Dictionary<SpellLine, List<Skill>>();
                if (living is GamePlayer player)
                {
                    lock (RecorderMgr._playerSpellCache)
                    {
                        if (RecorderMgr._playerSpellCache.TryGetValue(player.InternalID, out List<Spell> spells))
                        {
                            dict.Add(m_line, spells.Cast<Skill>().ToList());
                        }
                    }
                }
                return dict;
            }

            // Sagt dem Paket-Handler, dass diese Line zum Client gesendet werden muss
            public override List<SpellLine> GetSpellLinesForLiving(GameLiving living)
            {
                return new List<SpellLine> { m_line };
            }
        }
        #endregion
    }

    /// <summary>
    /// Modell für eine einzelne Aktion innerhalb eines Makros.
    /// </summary>
    [Serializable]
    public class RecorderAction
    {
        public string Type { get; set; }
        public int ID { get; set; }
        public string Value { get; set; }
    }
}