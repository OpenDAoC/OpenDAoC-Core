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
        // Message: /message <all|none|advice|alliance|announce|battlegroup|bgleader|broadcast|centersys|chat|command|cmddesc|cmdheader|
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
            if (args.Length < 4)
            {
                DisplaySyntax(client);
                return;
            }

            string type;

            switch (args[1].ToLower())
            {
                case "none":
                {
                    type = "None";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.None, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "advice":
                {
                    type = "Advice";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Advice, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "alliance":
                {
                    type = "Alliance";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Alliance, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "announce":
                {
                    type = "Announce";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Announce, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "battlegroup":
                {
                    type = "Battlegroup";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Battlegroup, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "bgleader":
                {
                    type = "BGLeader";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.BGLeader, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "broadcast":
                {
                    type = "Broadcast";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Broadcast, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "centersys":
                {
                    type = "CenterSys";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CenterSys, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "chat":
                {
                    type = "Chat";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Chat, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "command":
                {
                    type = "Command";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Command, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "cmddesc":
                {
                    type = "CmdDesc";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdDesc, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "cmdheader":
                {
                    type = "CmdHeader";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdHeader, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "cmdsyntax":
                {
                    type = "CmdSyntax";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "cmdusage":
                {
                    type = "CmdUsage";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdUsage, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "damageaddsh":
                {
                    type = "DamageAddSh";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.DamageAddSh, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "damaged":
                {
                    type = "Damaged";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Damaged, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "debug":
                {
                    type = "Debug";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "emote":
                {
                    type = "Emote";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Emote, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "emotesysothers":
                {
                    type = "EmoteSysOthers";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.EmoteSysOthers, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "error":
                {
                    type = "Error";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "expires":
                {
                    type = "Expires";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Expires, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "failed":
                {
                    type = "Failed";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Failed, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "group":
                {
                    type = "Group";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Group, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "guild":
                {
                    type = "Guild";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "help":
                {
                    type = "Help";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Help, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "important":
                {
                    type = "Important";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Important, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "killedbyalb":
                {
                    type = "KilledByAlb";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.KilledByAlb, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "killedbyhib":
                {
                    type = "KilledByHib";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.KilledByHib, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "killedbymid":
                {
                    type = "KilledByMid";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.KilledByMid, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "lfg":
                {
                    type = "LFG";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.LFG, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "loot":
                {
                    type = "Loot";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Loot, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "merchant":
                {
                    type = "Merchant";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Merchant, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "message":
                {
                    type = "Message";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Message, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "missed":
                {
                    type = "Missed";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Missed, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "officer":
                {
                    type = "Officer";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Officer, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "otherscombat":
                {
                    type = "OthersCombat";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.OthersCombat, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "playerdied":
                {
                    type = "PlayerDied";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.PlayerDied, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "playerdiedothers":
                {
                    type = "PlayerDiedOthers";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.PlayerDiedOthers, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "pulse":
                {
                    type = "Pulse";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Pulse, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "resisted":
                {
                    type = "Resisted";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Resisted, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "say":
                {
                    type = "Say";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Say, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "screencenter":
                {
                    type = "ScreenCenter";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.ScreenCenter, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "send":
                {
                    type = "Send";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Send, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "skill":
                {
                    type = "Skill";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Skill, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "spell":
                {
                    type = "Spell";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Spell, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "staff":
                {
                    type = "Staff";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Staff, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "success":
                {
                    type = "Success";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "sysarea":
                {
                    type = "SysArea";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.SysArea, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "sysothers":
                {
                    type = "SysOthers";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.SysOthers, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "system":
                {
                    type = "System";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.System, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "team":
                {
                    type = "Team";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Team, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "trade":
                {
                    type = "Trade";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Trade, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "yell":
                {
                    type = "Yell";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Yell, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "youdied":
                {
                    type = "YouDied";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.YouDied, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "youhit":
                {
                    type = "YouHit";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.YouHit, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "youwerehit":
                {
                    type = "YouWereHit";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.YouWereHit, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "dialogwarn":
                {
                    type = "DialogWarn";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.DialogWarn, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                case "all":
                {
                    type = "None";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.None, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Advice";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Advice, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Alliance";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Alliance, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Announce";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Announce, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Battlegroup";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Battlegroup, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "BGLeader";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.BGLeader, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Broadcast";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Broadcast, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "CenterSys";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CenterSys, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Chat";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Chat, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Command";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Command, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "CmdDesc";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdDesc, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "CmdHeader";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdHeader, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "CmdSyntax";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "CmdUsage";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.CmdUsage, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "DamageAddSh";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.DamageAddSh, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Damaged";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Damaged, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Debug";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Emote";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Emote, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "EmoteSysOthers";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.EmoteSysOthers, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Error";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Expires";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Expires, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Failed";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Failed, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Group";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Help";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Help, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Important";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Important, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "KilledByAlb";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.KilledByAlb, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "KilledByHib";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.KilledByHib, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "KilledByMid";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.KilledByMid, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "LFG";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.LFG, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Loot";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Loot, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Merchant";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Merchant, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Message";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Message, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Missed";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Missed, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Officer";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Officer, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "OthersCombat";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.OthersCombat, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "PlayerDied";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.PlayerDied, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "PlayerDiedOthers";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.PlayerDiedOthers, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Pulse";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Pulse, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Resisted";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Resisted, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Say";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Say, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "ScreenCenter";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.ScreenCenter, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Send";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Send, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Skill";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Skill, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Spell";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Spell, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Staff";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Staff, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Success";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "SysArea";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.SysArea, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "SysOthers";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.SysOthers, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "System";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.System, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Team";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Team, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Trade";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Trade, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "Yell";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.Yell, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "YouDied";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.YouDied, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "YouHit";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.YouHit, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "YouWereHit";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.YouWereHit, client, "GMCommands.Message.Msg.TestMessage", type);

                    type = "DialogWarn";

                    // Message: This is a test of the {0} message type.
                    ChatUtil.SendTypeMessage((int)eMsg.DialogWarn, client, "GMCommands.Message.Msg.TestMessage", type);
                }
                    break;
                default:
                    DisplaySyntax(client);
                    break;
            }
        }
    }
}
