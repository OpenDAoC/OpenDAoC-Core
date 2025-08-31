using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    [SkillHandlerAttribute(Abilities.Camouflage)]
    public class CamouflageSpecHandler : IAbilityActionHandler
    {
        public const int DISABLE_DURATION = 600000; // 1.65, 10min cooldown.

        public void Execute(Ability ab, GamePlayer player)
        {
            if (!player.IsStealthed)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Camouflage.NotStealthed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            ECSGameEffect camouflage = EffectListService.GetEffectOnTarget(player, eEffect.Camouflage);

            if (camouflage != null)
            {
                camouflage.Stop();
                return;
            }

            ECSGameEffectFactory.Create(new(player, 0, 1), static (in ECSGameEffectInitParams i) => new CamouflageECSGameEffect(i));
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Camouflage.UseCamo"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
