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
using log4net;
using DOL.GS.Keeps;

namespace DOL.GS.Commands
{
    /// <summary>
    /// A command to manage test out message types for the SendTypeMessage method.
    /// </summary>
    /// <author>Deftness</author>
    [CmdAttribute(
        "&message",
        // Message: '/message' - Tests the output of various message types used for the implementation of the SendMessageType standardization method.
        "GMCommands.Message.CmdList.Description",
        // Message: <----- '/{0}' Command {1}----->
        "AllCommands.Header.General.Commands",
        // Required minimum privilege level to use the command
        ePrivLevel.GM,
        // Message: Tests the output of various message types used for the implementation of the SendMessageType standardization method.
        "GMCommands.Message.Description",
        // Message: /message <none|advice|alliance|announce|battlegroup|bgleader|broadcast|centersys|chat|command|cmddesc|cmdheader|
        // cmdsyntax|cmdusage|damageaddsh|damaged|debug|emote|emotesysothers|error|expires|failed|group|guild|help|important|killedbyalb|
        // killedbyhib|killedbymid|lfg|loot|merchant|message|missed|officer|otherscombat|playerdied|playerdiedothers|pulse|resisted|say|
        // screencenter|send|skill|spell|staff|success|sysarea|sysothers|system|team|trade|yell|youdied|youhit|youwerehit|dialogwarn>
        "GMCommands.Message.Syntax.Message",
        // Message: Triggers a message of the specified type to test outputs.
        "GMCommands.Message.Usage.Message"
    )]
    public class MessageCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /// <summary>
        /// Handles the slash command.
        /// </summary>
        /// <param name="client">The client executing the command.</param>
        /// <param name="args">The arguments associated with the command.</param>
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 1)
            {
                DisplaySyntax(client);
                return;
            }

            if (args.Length == 1)
            {
                Enum.TryParse<eMsg>(args[1], true, out eMsg result);

                // Message: This is a test of the {0} message type.
                ChatUtil.SendTypeMessage(result, client, "GMCommands.Message.Msg.TestMessage", args[1]);
            }

        }
    }
}
