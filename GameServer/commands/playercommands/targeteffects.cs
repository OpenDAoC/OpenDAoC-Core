using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [Cmd("&targeteffects",
        ePrivLevel.Player,
        "Display various combat related info about your target",
        "/targeteffects")]
    public class TargetEffectsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "targeteffects"))
                return;

            if (!TryValidateTarget(client, out GameLiving target))
                return;

            List<string> info = new();

            List<ECSGameEffect> activeConc = GameLoop.GetListForTick<ECSGameEffect>();
            List<ECSGameEffect> activeTemp = GameLoop.GetListForTick<ECSGameEffect>();
            List<ECSGameEffect> disabledConc = GameLoop.GetListForTick<ECSGameEffect>();
            List<ECSGameEffect> disabledTemp = GameLoop.GetListForTick<ECSGameEffect>();
            List<ECSGameEffect> negative = GameLoop.GetListForTick<ECSGameEffect>();
            List<ECSGameEffect> nonSpellEffects = GameLoop.GetListForTick<ECSGameEffect>();

            List<ECSGameEffect> effects = target.effectListComponent.GetEffects();

            foreach (var effect in effects)
            {
                if (effect is ECSGameSpellEffect spellEffect)
                {
                    if (!spellEffect.SpellHandler.Spell.IsHarmful)
                    {
                        bool isConc = spellEffect.IsConcentrationEffect();
                        bool isActive = spellEffect.IsActive;

                        if (isConc && isActive)
                            activeConc.Add(spellEffect);
                        else if (!isConc && isActive)
                            activeTemp.Add(spellEffect);
                        else if (isConc && !isActive)
                            disabledConc.Add(spellEffect);
                        else // (!isConc && !isActive)
                            disabledTemp.Add(spellEffect);
                    }
                    else
                        negative.Add(spellEffect);
                }
                else
                    nonSpellEffects.Add(effect);
            }

            AddSection(info, "+ Active Concentration:", activeConc);
            AddSection(info, "+ Active Temporary:", activeTemp);
            AddSection(info, "+ Disabled Concentration:", disabledConc);
            AddSection(info, "+ Disabled Temporary:", disabledTemp);
            AddSection(info, "+ Negative:", negative);
            AddSection(info, "+ Non-Spell:", nonSpellEffects);

            client.Out.SendCustomTextWindow($"[{target.Name} effects]", info);
        }

        static bool TryValidateTarget(GameClient client, out GameLiving target)
        {
            target = (client.Player.TargetObject as GameLiving) ?? client.Player;

            if (target == null)
            {
                client.Out.SendMessage("No target or invalid target selected.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if ((ePrivLevel) client.Account.PrivLevel <= ePrivLevel.Player && client.Player != target && target is GamePlayer)
            {
                client.Out.SendMessage("This command cannot be used on another player.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            return true;
        }

        private static void AddSection(List<string> info, string header, List<ECSGameEffect> effects)
        {
            if (effects.Count == 0)
                return;

            info.Add(header);

            foreach (var effect in effects)
                AddEffect(info, effect);

            info.Add("");
        }

        private static void AddEffect(List<string> info, ECSGameEffect effect)
        {
            ECSGameSpellEffect spellEffect = effect as ECSGameSpellEffect;
            string effectName = effect.Name;
            string effectTypeStr = effect.EffectType.ToString();
            string details = string.Empty;

            if (effect.EffectType is not (eEffect.OffensiveProc or eEffect.DefensiveProc))
            {
                if (spellEffect != null)
                {
                    double value = spellEffect.SpellHandler.Spell.Value > 0 ? spellEffect.SpellHandler.Spell.Value : spellEffect.SpellHandler.Spell.Damage;

                    if (value != 0)
                        details += $"{value:0.##}  |  ";
                }

                if (effect.Effectiveness != 1.0)
                    details += $"x{effect.Effectiveness:0.##}  |  ";
            }

            long remaining = effect.GetRemainingTimeForClient();

            if (remaining > 0)
                details += $"{remaining / 1000}s  |  ";

            if (spellEffect != null)
                details += spellEffect.SpellHandler.Caster.Name;

            info.Add($"  {effectName}  ({effectTypeStr})");

            if (!string.IsNullOrEmpty(details))
                info.Add($"    {details}");
        }
    }
}
