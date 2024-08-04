using System.Linq;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.SkillHandler
{
    [SkillHandlerAttribute(Abilities.Intercept)]
    public class InterceptAbilityHandler : IAbilityActionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const int INTERCEPT_DISTANCE = 128;
        public const int REUSE_TIMER = 60 * 1000;

        public void Execute(Ability ab, GamePlayer player)
        {
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in InterceptAbilityHandler.");

                return;
            }

            if (player.TargetObject is not GameLiving target)
            {
                foreach (InterceptECSGameEffect intercept in player.effectListComponent.GetAbilityEffects().Where(e => e.EffectType is eEffect.Intercept))
                {
                    if (intercept.Source == player)
                        EffectService.RequestImmediateCancelEffect(intercept);
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CancelTargetNull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (target == player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CannotUse.CantInterceptYourself"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Group group = player.Group;

            if (group == null || !group.IsInTheGroup(target))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CannotUse.NotInGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CheckExistingEffectsOnTarget(player, target, true, out bool foundOurEffect, out InterceptECSGameEffect existingEffectFromAnotherSource);

            if (foundOurEffect)
                return;

            if (existingEffectFromAnotherSource != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CannotUse.InterceptTargetAlreadyInterceptedEffect", existingEffectFromAnotherSource.Source.GetName(0, true), existingEffectFromAnotherSource.Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CancelOurEffectThenAddOnTarget(player, target);
            player.DisableSkill(ab, REUSE_TIMER);
        }

        public static void CheckExistingEffectsOnTarget(GameLiving source, GameLiving target, bool cancelOurs, out bool foundOurEffect, out InterceptECSGameEffect effectFromAnotherSource)
        {
            foundOurEffect = false;
            effectFromAnotherSource = null;

            foreach (InterceptECSGameEffect intercept in target.effectListComponent.GetAbilityEffects().Where(e => e.EffectType is eEffect.Intercept))
            {
                if (intercept.Source == source)
                {
                    foundOurEffect = true;

                    if (cancelOurs)
                        EffectService.RequestImmediateCancelEffect(intercept);
                }

                if (intercept.Target == target)
                    effectFromAnotherSource = intercept;
            }
        }

        public static void CancelOurEffectThenAddOnTarget(GameLiving source, GameLiving target)
        {
            foreach (InterceptECSGameEffect intercept in source.effectListComponent.GetAbilityEffects().Where(e => e.EffectType is eEffect.Intercept))
            {
                if (intercept.Source == source)
                    EffectService.RequestImmediateCancelEffect(intercept);
            }

            new InterceptECSGameEffect(new ECSGameEffectInitParams(source, 0, 1, null), source, target);
        }
    }
}