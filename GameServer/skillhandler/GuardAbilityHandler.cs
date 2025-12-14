using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    [SkillHandler(Abilities.Guard)]
    public class GuardAbilityHandler : IAbilityActionHandler
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int GUARD_DISTANCE = 256;

        public void Execute(Ability ab, GamePlayer player)
        {
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in GuardAbilityHandler.");

                return;
            }

            if (player.TargetObject is not GameLiving target)
            {
                foreach (GuardECSGameEffect guard in player.effectListComponent.GetAbilityEffects(eEffect.Guard))
                {
                    if (guard.Source == player)
                        guard.End();
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CancelTargetNull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (target == player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.GuardTargetIsGuardSource"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Group group = player.Group;

            if (group == null || !group.IsInTheGroup(target))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.NotInGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CheckExistingEffectsOnTarget(player, target, true, out bool foundOurEffect, out GuardECSGameEffect existingEffectFromAnotherSource);

            if (foundOurEffect)
                return;

            if (existingEffectFromAnotherSource != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.GuardTargetAlreadyGuarded", existingEffectFromAnotherSource.Source.GetName(0, true), existingEffectFromAnotherSource.Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CancelOurEffectThenAddOnTarget(player, target);
        }

        public static void CheckExistingEffectsOnTarget(GameLiving source, GameLiving target, bool cancelOurs, out bool foundOurEffect, out GuardECSGameEffect effectFromAnotherSource)
        {
            foundOurEffect = false;
            effectFromAnotherSource = null;

            foreach (GuardECSGameEffect guard in target.effectListComponent.GetAbilityEffects(eEffect.Guard))
            {
                if (guard.Source == source)
                {
                    foundOurEffect = true;

                    if (cancelOurs)
                        guard.End();
                }

                if (guard.Target == target)
                    effectFromAnotherSource = guard;
            }
        }

        public static void CancelOurEffectThenAddOnTarget(GameLiving source, GameLiving target)
        {
            foreach (GuardECSGameEffect guard in source.effectListComponent.GetAbilityEffects(eEffect.Guard))
            {
                if (guard.Source == source)
                    guard.End();
            }

            ECSGameEffectFactory.Create(new(source, 0, 1), source, target, static (in i, source, target) => new GuardECSGameEffect(i, source, target));
        }
    }
}
