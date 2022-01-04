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
 * Autore: Krusck
 * Server: Clan cK
 * Edited by FinalFury for easier use.
 */

using System;
using System.Text;
using DOL.Language;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    /// <summary>
    /// Command handler for the /killself command
    /// </summary>
    [CmdAttribute(
        "&suicide",
        ePrivLevel.Admin,
        "Kill yourself. You can't suicide while in combat!")]
    public class KillselfCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player.InCombat)
            {
                DisplayMessage(client, "You can't kill yourself while in combat!");
                return;
            } else if (client.Player.CurrentZone.IsRvR)
            {
                DisplayMessage(client, "There are other ways to die in the Frontiers.");
                return;
            }
            else if (!client.Player.IsAlive)
            {
                DisplayMessage(client, "You are already dead!");
                return;
            }
            else
            {
                client.Out.SendCustomDialog("Do you want kill yourself?", new CustomDialogResponse(SuicideResponceHandler));
            }
        }
        protected virtual void SuicideResponceHandler(GamePlayer player, byte response)
        {
            //int amount = 10000;

            if (response == 1)
            {
                {
                    player.Emote(eEmote.SpellGoBoom);
                    player.TakeDamage(player, eDamageType.Natural, player.MaxHealth, 0);
                }
            }
            else
            {
                return;
            }

        }
    }
}
