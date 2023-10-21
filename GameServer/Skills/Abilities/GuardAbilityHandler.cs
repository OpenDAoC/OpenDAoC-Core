using System.Linq;
using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using log4net;

namespace Core.GS.SkillHandler
{
    [SkillHandler(Abilities.Guard)]
    public class GuardAbilityHandler : IAbilityActionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const int GUARD_DISTANCE = 256;

        public void Execute(Ability ab, GamePlayer player)
        {
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in GuardAbilityHandler.");

                return;
            }

            if (player.TargetObject == null)
            {
                foreach (GuardEcsAbilityEffect guard in player.effectListComponent.GetAllEffects().Where(e => e.EffectType == EEffect.Guard))
                {
                    if (guard.GuardSource == player)
                        EffectService.RequestImmediateCancelEffect(guard);
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CancelTargetNull"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            GamePlayer guardTarget = player.TargetObject as GamePlayer;

            if (guardTarget == player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.GuardTargetIsGuardSource"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            GroupUtil group = player.Group;

            if (guardTarget == null || group == null || !group.IsInTheGroup(guardTarget))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.NotInGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            // Cancel our effect if it exists and check if someone is already guarding the target.
            CheckExistingEffectsOnTarget(player, guardTarget, true, out bool foundOurEffect, out GuardEcsAbilityEffect existingEffectFromAnotherSource);

            if (foundOurEffect)
                return;

            if (existingEffectFromAnotherSource != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.GuardTargetAlreadyGuarded", existingEffectFromAnotherSource.GuardSource.GetName(0, true), existingEffectFromAnotherSource.GuardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            // Cancel other guard effects by this player before adding a new one.
            CancelOurEffectThenAddOnTarget(player, guardTarget);
        }

        public static void CheckExistingEffectsOnTarget(GameLiving guardSource, GameLiving guardTarget, bool cancelOurs, out bool foundOurEffect, out GuardEcsAbilityEffect effectFromAnotherSource)
        {
            foundOurEffect = false;
            effectFromAnotherSource = null;

            foreach (GuardEcsAbilityEffect guard in guardTarget.effectListComponent.GetAllEffects().Where(e => e.EffectType == EEffect.Guard))
            {
                if (guard.GuardSource == guardSource)
                {
                    foundOurEffect = true;

                    if (cancelOurs)
                        EffectService.RequestImmediateCancelEffect(guard);
                }

                if (guard.GuardTarget == guardTarget)
                    effectFromAnotherSource = guard;
            }
        }

        public static void CancelOurEffectThenAddOnTarget(GameLiving guardSource, GameLiving guardTarget)
        {
            foreach (GuardEcsAbilityEffect guard in guardSource.effectListComponent.GetAllEffects().Where(e => e.EffectType == EEffect.Guard))
            {
                if (guard.GuardSource == guardSource)
                    EffectService.RequestImmediateCancelEffect(guard);
            }

            new GuardEcsAbilityEffect(new EcsGameEffectInitParams(guardSource, 0, 1, null), guardSource, guardTarget);
        }
    }
}
