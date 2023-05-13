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
using System.Linq;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&heal",
		// Message:
		"AdminCommands.DoorTeleport.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.Admin,
		// Message: Restores a target's HP, endurance, and power to full, as well as removing any negative effects.
		"GMCommands.Heal.Description",
		// Message: /heal
		"GMCommands.Heal.Syntax.Heal",
		// Message: Heals the target's HP, endurance, and power.
		"GMCommands.Heal.Usage.Heal",
		// Message: /heal me
		"GMCommands.Heal.Syntax.MeHeal",
		// Message: Restores the stats of the client executing the command.
		"GMCommands.Heal.Usage.MeHeal"
	)]
	public class HealCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			try
			{
				GameLiving target = client.Player.TargetObject as GameLiving;
				
				if (target == null || (args.Length > 1 && args[1].ToLower() == "me"))
					target = (GameLiving)client.Player;

				target.Health = target.MaxHealth;
				target.Endurance = target.MaxEndurance;
				// TODO: Refactor references of "Mana" to "Power"
				target.Mana = target.MaxMana;

				if (target.effectListComponent.ContainsEffectForEffectType(eEffect.ResurrectionIllness))
				{
					EffectService.RequestCancelEffect(target.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == eEffect.ResurrectionIllness));
				}
				
				if (target.effectListComponent.ContainsEffectForEffectType(eEffect.RvrResurrectionIllness))
				{
					EffectService.RequestCancelEffect(target.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == eEffect.RvrResurrectionIllness));
				}

				if (target is GamePlayer targetPlayer)
				{
					// Message: You feel refreshed and cured of all maladies!
					ChatUtil.SendTypeMessage((int)eMsg.Spell, targetPlayer, "GMCommands.Heal.Msg.YouFeelCured", null);

					// Force an update of the target's status so that the heal is registered in the UI more quickly
					targetPlayer.UpdatePlayerStatus();
				}
			}
			catch (Exception)
			{
				DisplaySyntax(client);
			}
		}
	}
}