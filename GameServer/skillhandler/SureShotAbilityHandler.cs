using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Handler for Sure Shot ability
    /// </summary>
    [SkillHandlerAttribute(Abilities.SureShot)]
    public class SureShotAbilityHandler : IAbilityActionHandler
    {
        public void Execute(Ability ab, GamePlayer player)
        {
            if (EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot) is SureShotECSGameEffect sureShot)
            {
                sureShot.Stop();
                return;
            }

            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.SureShot.CannotUseDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            RapidFireECSGameEffect rapidFire = EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire) as RapidFireECSGameEffect;
            rapidFire?.Stop(false);

            TrueShotECSGameEffect trueShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot) as TrueShotECSGameEffect;
            trueShot?.Stop(false);

            ECSGameEffectFactory.Create(new(player, 0, 1), static (in ECSGameEffectInitParams i) => new SureShotECSGameEffect(i));
        }
    }
}
