/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.RealmAbilities;
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
			Console.WriteLine();
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

			// cancel rapid fire effect
			RapidFireECSGameEffect rapidFire = (RapidFireECSGameEffect)EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire);
			if (rapidFire != null)
				EffectService.RequestImmediateCancelEffect(rapidFire, false);

			// cancel sure shot effect
			SureShotECSGameEffect sureShot = (SureShotECSGameEffect)EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot);
			if (sureShot != null)
				EffectService.RequestImmediateCancelEffect(sureShot);

			TrueShotECSGameEffect trueshot = (TrueShotECSGameEffect)EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot);
			if (trueshot != null)
				EffectService.RequestImmediateCancelEffect(trueshot, false);

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
			player.attackComponent.RequestStartAttack(player.TargetObject);
			
			DisableSkill(player);
		}

		public AtlasOF_Longshot(DbAbilities ability, int level) : base(ability, level)
		{
		}
	}
}
