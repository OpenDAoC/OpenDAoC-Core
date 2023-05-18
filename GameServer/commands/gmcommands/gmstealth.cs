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
using System.Collections.Generic;
using DOL.GS.Commands;
using DOL.GS;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gmstealth",
        // Message: '/gmstealth' - Activates/Deactivates a GM/Admin's ability to remain invisible to all players.
        "GMCommands.AddBind.CmdList.Description",
        // Message: <----- '/{0}' Command {1}----->
        "AllCommands.Header.General.Commands",
        // Required minimum privilege level to use the command
        ePrivLevel.GM,
        // Message: Controls a GM/Admin's visibility to Players. Does not make a character invisible to clients with a privilege level of 2+.
        "GMCommands.GMStealth.Description",
        // Message: /gmstealth <on|off>
        "GMCommands.GMStealth.Syntax.OnOff",
        // Message: Activates/Deactivates a GM/Admin's visibility to Players.
        "GMCommands.GMStealth.Usage.OnOff"
    )]
    public class GMStealthCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
        	if (args.Length != 2) {
        		DisplaySyntax(client);
        	}
        	else if (args[1].ToLower().Equals("on")) {

                if (client.Player.IsStealthed != true)
                {
                   client.Player.Stealth(true);
                   client.Player.CurrentSpeed = 191;

                   // Message: You are now invisible to all clients with a privilege level of 1!
                   ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.GMStealth.Msg.InvisibleOn", null);
                }
        	}
            else if (args[1].ToLower().Equals("off"))
            {
                    client.Player.Stealth(false);

                    // Message: You are no longer invisible to Players!
                    ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.GMStealth.Msg.InvisibleOff", null);
            }
        }
    }
}
