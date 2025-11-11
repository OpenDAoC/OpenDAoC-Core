using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Handler for Critical Shot ability
	/// </summary>
	[SkillHandlerAttribute(Abilities.Critical_Shot)]
	public class CriticalShotAbilityHandler : IAbilityActionHandler
	{
		public void Execute(Ability ab, GamePlayer player)
		{
			if (player.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.NoRangedWeapons"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
			}
			if (player.IsSitting)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.MustBeStanding"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                return;
			}

			RapidFireECSGameEffect rapidFire = EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire) as RapidFireECSGameEffect;
			rapidFire?.End();

			SureShotECSGameEffect sureShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot) as SureShotECSGameEffect;
			sureShot?.End();

			TrueShotECSGameEffect trueShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot) as TrueShotECSGameEffect;
			trueShot?.End();

			ECSGameEffect volley = EffectListService.GetEffectOnTarget(player, eEffect.Volley);

			if (volley != null)
			{
				player.Out.SendMessage("You can't use Critical-Shot while Volley is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (player.attackComponent.AttackState)
			{
				if (player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Critical)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CriticalShot.SwitchToRegular"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					player.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
				}
				else
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CriticalShot.AlreadyFiring"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				}
				return;
			}
			player.rangeAttackComponent.RangedAttackType = eRangedAttackType.Critical;
			player.attackComponent.RequestStartAttack();
		}
	}
}
