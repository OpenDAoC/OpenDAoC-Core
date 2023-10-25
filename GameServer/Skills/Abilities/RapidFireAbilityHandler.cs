using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Skills;

[SkillHandler(AbilityConstants.RapidFire)]
public class RapidFireAbilityHandler : IAbilityActionHandler
{
	public void Execute(Ability ab, GamePlayer player)
	{

		RapidFireEcsAbilityEffect rapidFire = (RapidFireEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.RapidFire);
		if (rapidFire!=null)
		{
			EffectService.RequestImmediateCancelEffect(rapidFire, false);
			return;
		}

		if(!player.IsAlive)
		{
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.RapidFire.CannotUseDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
		}

		SureShotEcsAbilityEffect sureShot = (SureShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.SureShot);
		if (sureShot != null)
			EffectService.RequestImmediateCancelEffect(sureShot);

		TrueShotEcsAbilityEffect trueshot = (TrueShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.TrueShot);
		if (trueshot != null)
			EffectService.RequestImmediateCancelEffect(trueshot, false);

		EcsGameEffect volley = EffectListService.GetEffectOnTarget(player, EEffect.Volley);
		if (volley != null)
		{
			player.Out.SendMessage("You can't use "+ab.Name+" while Volley is active!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		new RapidFireEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1));
	}
}