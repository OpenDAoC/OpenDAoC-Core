using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using Newtonsoft.Json;
using DOL.Logging;

namespace DOL.GS
{
    public class RecorderMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Dictionary<GamePlayer, List<RecorderAction>> _activeRecordings = new();

        public const string RecorderLineKey = "Recorder"; 
        public const string RecorderDisplayName = "Recorder"; // Name which is displayed ingame
        public const int RecorderBaseIcon = 11130; // Animist DD Icon as default

        #region Initialization
        public static bool Init()
        {
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
                    string dynamictooltip = "Recorded Actions:\n";
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
                                else if (action.Type == "Command")
                                {
                                    dynamictooltip += $"Cmd: {action.ID}\n";
                                }
                            }
                        }
                    }
                    catch { dynamictooltip = "Recorded Macro (Data Error)"; }
                    


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

        // This gets called from /recorder start command
        public static void StartRecording(GamePlayer player)
        {
            lock (_activeRecordings) _activeRecordings[player] = new List<RecorderAction>();
            player.Out.SendMessage("Recording started...", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
        }

        // This gets called from CastingComponent/Style..../Ability...
        public static void RecordAction(GamePlayer player, Spell spell)
        {
            // Disable recording recorders
            if (spell is RecorderSpell || !IsPlayerRecording(player)) return;
            
            // Record spells/styles/...
            lock (_activeRecordings)
            {
                if (_activeRecordings.TryGetValue(player, out var actions))
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
                if (!_activeRecordings.TryGetValue(player, out var actions) || actions.Count == 0) return;
                
                // We set first spell icon as recorder icon
                int autoIconId = RecorderBaseIcon;
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
                player.Out.SendMessage($"Recorder '{name}' saved.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
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