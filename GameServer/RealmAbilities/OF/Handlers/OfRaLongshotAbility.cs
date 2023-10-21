using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.RealmAbilities
{
	public class OfRaLongshotAbility : TimedRealmAbility
	{
		
		public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 6; }
		public override int GetReUseDelay(int level) { return 300; } // 5 mins
		
		public override void Execute(GameLiving living)
		{
			Console.WriteLine();
			if (living is not GamePlayer player) return;
			if (player.ActiveWeaponSlot != EActiveWeaponSlot.Distance)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.NoRangedWeapons"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                return;
			}
			if (player.IsSitting)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.MustBeStanding"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                return;
			}

			// cancel rapid fire effect
			RapidFireEcsAbilityEffect rapidFire = (RapidFireEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.RapidFire);
			if (rapidFire != null)
				EffectService.RequestImmediateCancelEffect(rapidFire, false);

			// cancel sure shot effect
			SureShotEcsAbilityEffect sureShot = (SureShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.SureShot);
			if (sureShot != null)
				EffectService.RequestImmediateCancelEffect(sureShot);

			TrueShotEcsAbilityEffect trueshot = (TrueShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.TrueShot);
			if (trueshot != null)
				EffectService.RequestImmediateCancelEffect(trueshot, false);

			if (player.attackComponent.AttackState)
			{
				if (player.rangeAttackComponent.RangedAttackType == ERangedAttackType.Long)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CriticalShot.SwitchToRegular"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					player.rangeAttackComponent.RangedAttackType = ERangedAttackType.Normal;
				}
				else
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CriticalShot.AlreadyFiring"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				}
				return;
			}
			player.rangeAttackComponent.RangedAttackType = ERangedAttackType.Long;
			player.attackComponent.RequestStartAttack(player.TargetObject);
			
			DisableSkill(player);
		}

		public OfRaLongshotAbility(DbAbility ability, int level) : base(ability, level)
		{
		}
	}
}
