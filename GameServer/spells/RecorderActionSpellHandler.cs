using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.Database;
using Newtonsoft.Json;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.RecorderAction)]
    public class RecorderActionHandler : SpellHandler
    {
        // Wir nutzen den Konstruktor deines funktionierenden Beispiels
        public RecorderActionHandler(GameLiving caster, Spell spell, SpellLine line) 
            : base(caster, spell, line) { }

        public override bool StartSpell(GameLiving target)
        {
            if (Caster is GamePlayer player)
            {
                ExecuteMacro(player, Spell.Name);
            }
            return true;
        }

        private void ExecuteMacro(GamePlayer player, string macroName)
        {
            try 
            {
                // Datenbankabfrage: Wir holen das Makro des Spielers
                var dbEntry = GameServer.Database.SelectAllObjects<DBCharacterRecorder>()
                    .FirstOrDefault(r => r.CharacterID == player.InternalID && r.Name == macroName);

                if (dbEntry == null)
                {
                    MessageToCaster("Makro-Daten konnten nicht geladen werden.", eChatType.CT_System);
                    return;
                }

                var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(dbEntry.ActionsJson);
                if (actions == null || actions.Count == 0) return;

                MessageToCaster($"Führe Makro '{dbEntry.Name}' aus...", eChatType.CT_Spell);

                // Hier nutzen wir deinen Code-Schnipsel! 
                // Wir holen uns die Recorder-Line sicher aus dem System.
                // Falls SkillBase nicht die richtige Klasse ist, ersetze sie durch die Klasse, aus der dein Schnipsel stammt.
                SpellLine recorderLine = SkillBase.GetSpellLine(RecorderMgr.RecorderLineKey, true);

                foreach (var action in actions)
                {
                    if (action.Type == "Spell")
                    {
                        Spell s = SkillBase.GetSpellByID(action.ID);
                        if (s != null && recorderLine != null)
                        {
                            // Jetzt übergeben wir die explizit geholte Line
                            player.CastSpell(s, recorderLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECORDER ERROR] {ex.Message}");
            }
        }
    }
}