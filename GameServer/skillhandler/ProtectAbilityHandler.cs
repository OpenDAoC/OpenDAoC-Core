using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    [SkillHandlerAttribute(Abilities.Protect)]
    public class ProtectAbilityHandler : IAbilityActionHandler
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int PROTECT_DISTANCE = 1000;

        public void Execute(Ability ab, GamePlayer player)
        {
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in ProtectAbilityHandler.");

                return;
            }

            if (player.TargetObject is not GameLiving target)
            {
                foreach (ProtectECSGameEffect protect in player.effectListComponent.GetAbilityEffects(eEffect.Protect))
                {
                    if (protect.Source == player)
                        protect.Stop();
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CancelTargetNull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (target == player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CannotUse.CantProtectYourself"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Group group = player.Group;

            if (group == null || !group.IsInTheGroup(target))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CannotUse.NotInGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CheckExistingEffectsOnTarget(player, target, true, out bool foundOurEffect, out ProtectECSGameEffect existingEffectFromAnotherSource);

            if (foundOurEffect)
                return;

            if (existingEffectFromAnotherSource != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CannotUse.ProtectTargetAlreadyProtectEffect", existingEffectFromAnotherSource.Source.GetName(0, true), existingEffectFromAnotherSource.Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CancelOurEffectThenAddOnTarget(player, target);
        }

        public static void CheckExistingEffectsOnTarget(GameLiving source, GameLiving target, bool cancelOurs, out bool foundOurEffect, out ProtectECSGameEffect effectFromAnotherSource)
        {
            foundOurEffect = false;
            effectFromAnotherSource = null;

            foreach (ProtectECSGameEffect protect in target.effectListComponent.GetAbilityEffects(eEffect.Protect))
            {
                if (protect.Source == source)
                {
                    foundOurEffect = true;

                    if (cancelOurs)
                        protect.Stop();
                }

                if (protect.Target == target)
                    effectFromAnotherSource = protect;
            }
        }

        public static void CancelOurEffectThenAddOnTarget(GameLiving source, GameLiving target)
        {
            foreach (ProtectECSGameEffect protect in source.effectListComponent.GetAbilityEffects(eEffect.Protect))
            {
                if (protect.Source == source)
                    protect.Stop();
            }

            new ProtectECSGameEffect(new ECSGameEffectInitParams(source, 0, 1, null), source, target);
        }
    }
}