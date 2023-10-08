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

using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Handler for Sure Shot ability
	/// </summary>
	[SkillHandler(Abilities.SureShot)]
	public class SureShotAbilityHandler : IAbilityActionHandler
	{
		public void Execute(Ability ab, GamePlayer player)
		{
			SureShotEcsAbilityEffect sureShot = (SureShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.SureShot);
			if (sureShot != null)
			{
				EffectService.RequestImmediateCancelEffect(sureShot);
				return;
			}

			if (!player.IsAlive)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.SureShot.CannotUseDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			RapidFireEcsAbilityEffect rapidFire = (RapidFireEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.RapidFire);
			if (rapidFire != null)
				EffectService.RequestImmediateCancelEffect(rapidFire, false);

			TrueShotEcsAbilityEffect trueshot = (TrueShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.TrueShot);
			if (trueshot != null)
				EffectService.RequestImmediateCancelEffect(trueshot, false);

			new SureShotEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1));
		}
	}
}
