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
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
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

			RapidFireECSGameEffect rapidFire = (RapidFireECSGameEffect)EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire);
			if (rapidFire!=null)
			{
				EffectService.RequestImmediateCancelEffect(rapidFire, false);
				return;
			}

			if(!player.IsAlive)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.RapidFire.CannotUseDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
			}

			SureShotECSGameEffect sureShot = (SureShotECSGameEffect)EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot);
			if (sureShot != null)
				EffectService.RequestImmediateCancelEffect(sureShot);

			TrueShotECSGameEffect trueshot = (TrueShotECSGameEffect)EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot);
			if (trueshot != null)
				EffectService.RequestImmediateCancelEffect(trueshot, false);

			ECSGameEffect volley = EffectListService.GetEffectOnTarget(player, eEffect.Volley);
			if (volley != null)
			{
				player.Out.SendMessage("You can't use "+ab.Name+" while Volley is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			new RapidFireECSGameEffect(new ECSGameEffectInitParams(player, 0, 1));
		}
	}
}
