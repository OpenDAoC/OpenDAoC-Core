using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;
using Newtonsoft.Json;
using DOL.Logging;
using System.Reflection;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Handles the execution of recorded macro actions for the Recorder System.
    /// </summary>
    [SpellHandler(eSpellType.RecorderAction)]
    public class RecorderActionHandler : SpellHandler
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public RecorderActionHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override bool StartSpell(GameLiving target)
        {
            if (!(Caster is GamePlayer player))
                return false;

            // 1. Standard Execution Path
            // Check if the spell object passed by the core is already our RecorderSpell type.
            if (this.Spell is RecorderMgr.RecorderSpell mySpell)
            {
                ExecuteMacro(player, mySpell.RecordData);
                return true;
            }

            // 2. Fallback Mechanism
            // If the core provides a generic Spell object, we attempt to find the matching 
            // macro in the player's custom spell list via the ID.
            int requestedId = this.Spell.ID;
            var fallbackSpell = player.SpellMacros?.FirstOrDefault(s => s.ID == requestedId) as RecorderMgr.RecorderSpell;

            if (fallbackSpell != null)
            {
                ExecuteMacro(player, fallbackSpell.RecordData);
            }
            else
            {
                // Log the failure for server administration instead of flooding player chat
                if (log.IsWarnEnabled)
                    log.Warn($"[Recorder] Player {player.Name} tried to execute Macro ID {requestedId}, but it was not found in their SpellMacros list.");
                
                player.Out.SendMessage("An error occurred while trying to execute the macro.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
            }

            return true;
        }

        /// <summary>
        /// Deserializes and executes the actions stored within the DB entry.
        /// </summary>
        private void ExecuteMacro(GamePlayer player, DBCharacterRecorder dbEntry)
        {
            if (dbEntry == null || string.IsNullOrEmpty(dbEntry.ActionsJson))
                return;

            try
            {
                var actions = JsonConvert.DeserializeObject<List<RecorderAction>>(dbEntry.ActionsJson);
                if (actions == null || actions.Count == 0)
                    return;

                SpellLine recorderLine = SkillBase.GetSpellLine(RecorderMgr.RecorderLineKey);

                foreach (var action in actions)
                {
                    if (action.Type == "Spell")
                    {
                        Spell s = SkillBase.GetSpellByID(action.ID);
                        if (s != null)
                        {
                            // Trigger the spell casting component
                            player.castingComponent.RequestCastSpell(s, recorderLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Error executing Recorder Macro for {player.Name}: {ex.Message}", ex);
            }
        }
    }
}