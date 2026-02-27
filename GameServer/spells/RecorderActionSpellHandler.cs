using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS;
using Newtonsoft.Json;
using DOL.Logging;
using System.Reflection;
using DOL.GS.Styles;

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
            if (Caster is not GamePlayer player)
                return false;
            // 1. Standard Execution Path
            // Check if the spell object passed by the core is already our RecorderSpell type.
            if (this.Spell is RecorderMgr.RecorderSpell mySpell)
            {
                ExecuteMacro(player, mySpell);
                return true;
            }

            // 2. Fallback Mechanism
            // If the core provides a generic Spell object, we attempt to find the matching 
            // macro in the player's custom spell list via the ID.
            int requestedId = this.Spell.ID;
            var fallbackSpell = player.SpellMacros?.FirstOrDefault(s => s.ID == requestedId) as RecorderMgr.RecorderSpell;

            if (fallbackSpell != null)
            {
                ExecuteMacro(player, fallbackSpell);
                return true;
            }
            else
            {
                player.Out.SendMessage("An error occurred while trying to execute the macro.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
            }

            return false;
        }

        public override int CalculateEnduranceCost()
        {
            return 0;
        }

        /// <summary>
        /// Executes the actions stored within the DB entry.
        /// Uses the pre-parsed <see cref="RecorderMgr.RecorderSpell.Actions"/> list cached at
        /// spellbook build time to avoid JSON deserialization on every macro execution.
        /// Falls back to parsing <c>ActionsJson</c> directly if the cache is unavailable.
        /// </summary>
        private void ExecuteMacro(GamePlayer player, RecorderMgr.RecorderSpell recorderSpell)
        {
            // Prefer the cached list; fall back to raw JSON only as a safety net.
            List<RecorderAction> actions = recorderSpell.Actions;
            if (actions == null)
            {
                if (string.IsNullOrEmpty(recorderSpell.RecordData?.ActionsJson))
                    return;
                try { actions = JsonConvert.DeserializeObject<List<RecorderAction>>(recorderSpell.RecordData.ActionsJson); }
                catch (Exception ex)
                {
                    log.Error($"[RECORDER] Error parsing actions for {player.Name}: {ex.Message}", ex);
                    return;
                }
            }

            if (actions == null || actions.Count == 0)
                return;

            try
            {
                SpellLine recorderLine = SkillBase.GetSpellLine(RecorderMgr.RecorderLineKey);

                foreach (var action in actions)
                {
                    switch (action.Type)
                    {
                        case RecorderActionType.Spell:
                        {
                            Spell s = SkillBase.GetSpellByID(action.ID);
                            if (s != null)
                                player.castingComponent.RequestCastSpell(s, recorderLine);
                            break;
                        }
                        case RecorderActionType.Style:
                        {
                            Style style = SkillBase.GetStyleByID(action.ID, player.CharacterClass.ID);
                            if (style != null)
                                StyleProcessor.TryToUseStyle(player, style);
                            break;
                        }
                        case RecorderActionType.Ability:
                        {
                            // Prefer the normal ability ID first (this is what we recorded),
                            // fall back to internal-ID lookup only if needed.
                            Ability abil = SkillBase.GetAbility(action.ID)
                                        ?? SkillBase.GetAbilityByInternalID(action.ID);
                            if (abil != null)
                                player.castingComponent.RequestUseAbility(abil);
                            break;
                        }
                        case RecorderActionType.Command:
                        {
                            if (!string.IsNullOrEmpty(action.Value))
                                ScriptMgr.HandleCommand(player.Client, action.Value);
                            break;
                        }
                        case RecorderActionType.ItemCharge:
                        {
                            // Value is stored as "{type}:{itemName}". Parse the type from before
                            // the colon; fall back to primary charge (1) if parsing fails.
                            int chargeType = 1;
                            if (!string.IsNullOrEmpty(action.Value))
                            {
                                int sep = action.Value.IndexOf(':');
                                int.TryParse(sep >= 0 ? action.Value[..sep] : action.Value, out chargeType);
                                if (chargeType < 1) chargeType = 1;
                            }
                            player.UseSlot(action.ID, chargeType);
                            break;
                        }
                        case RecorderActionType.WeaponSwitch:
                        {
                            player.UseSlot(action.ID, 0);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"[RECORDER] Error executing macro for {player.Name}: {ex.Message}", ex);
            }
        }
    }
}