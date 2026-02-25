using System;
using DOL.GS.PacketHandler;
using DOL.GS.Commands;

namespace DOL.GS
{
    /// <summary>
    /// Implements the <c>/recorder</c> command for players.  This handler
    /// primarily forwards requests to <see cref="RecorderMgr"/> and
    /// displays usage information when invoked incorrectly.
    /// </summary>
    [CmdAttribute("&recorder", ePrivLevel.Player, "Recorder commands", "/recorder help")]
    public class RecorderCommandHandler : ICommandHandler
    {
        // All valid usage lines visible to the player when requesting help.
        private static readonly string[] UsageMessages =
        {
            // Eden like, just an example what could be implemented for now
            "Recorder Usage",
            "/recorder start : Start recording the next spells/styles/abilities/commands",
            "/recorder save <name> : Save previously recorded actions as <name>",
            //"/recorder sendkey <key> : Send specific key to the game client (ie: F, Space, etc)",
            "/recorder cancel : Cancel current recording",
            "/recorder icon <name> <icon_id> : Apply your next casted spell icon to record <name>, or a direct input <icon_id>",
            "/recorder list : List all recorded actions",
            "/recorder delete <name> : Remove record <name>",
            "/recorder rename <name> <newname> : Rename record <name> to <newname>",
            //"/recorder param <parameter_name> <parameter_value> : Store text parameters to replace in commands; e.g. /recorder param assistname Rtha will replace '%assistname' by 'Rtha' in /assist %assistname",
            //"/recorder param list : List all your text parameters",
            //"/recorder param delete <name> : Remove text parameter <name>",
            "/recorder import <character_name> <record_name>", // [dualspec: 1 or 2]
            //"/recorder info <name> : Display record information",
            //"/recorder discard <name> <index> : Remove a specific action",
            //"/recorder insert <name> <index> : Insert an action at the chosen position",
            //"/recorder append <name> : Shortcut to insert at the end",
            "/recorder help : Displays recorder usage"
        };

        /// <inheritdoc />
        public void OnCommand(GameClient client, string[] args)
        {
            if (client?.Player == null)
                return;

            if (args.Length < 2 || args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                SendUsage(client);
                return;
            }

            var action = args[1].ToLowerInvariant();

            switch (action)
            {
                case "start":
                    RecorderMgr.StartRecording(client.Player);
                    break;

                case "cancel":
                    if (!RecorderMgr.CancelRecording(client.Player))
                        client.Player.Out.SendMessage("No active recording to cancel.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    break;

                case "delete" when args.Length >= 3:
                    {
                        var name = args[2];
                        if (RecorderMgr.DeleteRecording(client.Player, name))
                            client.Player.Out.SendMessage($"Recorder '{name}' deleted.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        else
                            client.Player.Out.SendMessage($"Recorder '{name}' not found.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }
                    break;

                case "rename" when args.Length >= 4:
                    {
                        var oldName = args[2];
                        var newName = args[3];
                        if (RecorderMgr.RenameRecording(client.Player, oldName, newName))
                            client.Player.Out.SendMessage($"Recorder '{oldName}' renamed to '{newName}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        else
                            client.Player.Out.SendMessage($"Rename failed: either the original recorder was not found or the new name is already in use.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }
                    break;

                case "icon" when args.Length >= 3:
                    {
                        var name = args[2];
                        int? iconId = null;
                        if (args.Length >= 4)
                        {
                            if (int.TryParse(args[3], out var parsed))
                                iconId = parsed;
                            else
                            {
                                client.Player.Out.SendMessage($"Invalid icon ID '{args[3]}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                                break;
                            }
                        }

                        if (RecorderMgr.SetRecorderIcon(client.Player, name, iconId))
                        {
                            if (iconId.HasValue)
                                client.Player.Out.SendMessage($"Recorder '{name}' icon set to {iconId}.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            else
                                client.Player.Out.SendMessage($"Next spell cast will determine icon for '{name}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        }
                        else
                        {
                            client.Player.Out.SendMessage($"Recorder '{name}' not found.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        }
                    }
                    break;
                case "save" when args.Length >= 3:
                    RecorderMgr.StopAndSaveRecording(client.Player, args[2]);
                    break;
                case "import" when args.Length >= 4:
                    {
                        string sourceCharName = args[2];
                        string sourceRecorderName = args[3];
                        RecorderMgr.ImportRecorder(client.Player, sourceCharName, sourceRecorderName);
                    }
                    break;

                case "list":
                    RecorderMgr.ListAccountRecorders(client.Player);
                    break;

                default:
                    SendUsage(client);
                    break;
            }
        }

        /// <summary>
        /// Sends all lines from <see cref="UsageMessages"/> to the player.
        /// </summary>
        private static void SendUsage(GameClient client)
        {
            foreach (var msg in UsageMessages)
            {
                client.Player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}

