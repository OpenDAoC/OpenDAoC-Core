using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Handler for Critical Shot ability
	/// </summary>
	public class AtlasOF_Longshot : TimedRealmAbility
	{
		
		public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 6; }
		public override int GetReUseDelay(int level) { return 300; } // 5 mins
		
		public override void Execute(GameLiving living)
		{
			if (living is not GamePlayer player) return;
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
			rapidFire?.Stop();

			SureShotECSGameEffect sureShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot) as SureShotECSGameEffect;
			sureShot?.Stop();

			TrueShotECSGameEffect trueShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot) as TrueShotECSGameEffect;
			trueShot?.Stop();

			if (player.attackComponent.AttackState)
			{
				if (player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Long)
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
			player.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;
			player.attackComponent.RequestStartAttack();
			
			DisableSkill(player);
		}

		public AtlasOF_Longshot(DbAbility ability, int level) : base(ability, level)
		{
		}
	}
}
