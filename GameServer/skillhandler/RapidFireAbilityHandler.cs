using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Handler for Rapid Fire ability
    /// </summary>
    [SkillHandlerAttribute(Abilities.RapidFire)]
    public class RapidFireAbilityHandler : IAbilityActionHandler
    {
        public void Execute(Ability ab, GamePlayer player)
        {
            if (EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire) is RapidFireECSGameEffect rapidFire)
            {
                rapidFire.End(false);
                return;
            }

            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.RapidFire.CannotUseDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            SureShotECSGameEffect sureShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot) as SureShotECSGameEffect;
            sureShot?.End();

            TrueShotECSGameEffect trueShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot) as TrueShotECSGameEffect;
            trueShot?.End();

            ECSGameEffect volley = EffectListService.GetEffectOnTarget(player, eEffect.Volley);

            if (volley != null)
            {
                player.Out.SendMessage("You can't use "+ab.Name+" while Volley is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            ECSGameEffectFactory.Create(new(player, 0, 1), static (in ECSGameEffectInitParams i) => new RapidFireECSGameEffect(i));
        }
    }
}
