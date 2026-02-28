using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.Commands;
using DOL.GS.ServerProperties;

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
        // Short usage shown when the player types /recorder with no arguments or an unknown sub-command.
        private static readonly IList<string> HelpLines = new List<string>
        {
            "The Recorder lets you save a sequence of spells, styles, abilities and",
            "commands as a macro. The macro appears in your spellbook and can be",
            "placed on a quickbar button like any spell.",
            "",
            "--- Recording ---",
            "/recorder start",
            "  Begin a new recording session.",
            "/recorder save <name>",
            "  Save the recorded actions under the given name.",
            "/recorder cancel",
            "  Discard the current recording without saving.",
            "",
            "--- Managing recorders ---",
            "/recorder delete <name>",
            "  Permanently delete a recorder.",
            "/recorder rename <name> <newname>",
            "  Rename an existing recorder.",
            "/recorder icon <name>",
            "  Set the icon to your next cast spell.",
            "",
            "--- Editing actions ---",
            "/recorder insert <name> <index>",
            "  Insert your next action at the given position.",
            "/recorder append <name>",
            "  Insert your next action at the end.",
            "/recorder discard <name> <index>",
            "  Remove the action at the given position.",
            "",
            "--- Other ---",
            "/recorder list",
            "  Show all recorders on your account.",
            "/recorder import <character> <name>",
            "  Copy a recorder from another character on your account.",
        };

        private static readonly string[] UsageMessages =
        {
            // Eden like, just an example what could be implemented for now
            "Recorder Usage",
            "/recorder start : Start recording the next spells/styles/abilities/commands",
            "/recorder save <name> : Save previously recorded actions as <name>",
            //"/recorder sendkey <key> : Send specific key to the game client (ie: F, Space, etc)",         // This needs client adjustments
            "/recorder cancel : Cancel current recording",
            "/recorder icon <name> : Apply your next casted spell icon to recorder <name>",
            "/recorder list : List all recorded actions",   // Sends a window to the player, with all characters from the account and displays all recorders with max 3 actions of a recorder
            "/recorder delete <name> : Remove record <name>",
            "/recorder rename <name> <newname> : Rename record <name> to <newname>",

            // Need to check what param is really doing and if needed
            //"/recorder param <parameter_name> <parameter_value> : Store text parameters to replace in commands; e.g. /recorder param assistname Rtha will replace '%assistname' by 'Rtha' in /assist %assistname",
            //"/recorder param list : List all your text parameters",
            //"/recorder param delete <name> : Remove text parameter <name>",
            
            "/recorder import <character_name> <record_name>", // [dualspec: 1 or 2]
            //"/recorder info <name> : Display record information", // For what should we use this, if you right click you already get all info
            "/recorder discard <name> <index> : Remove a specific action",
            "/recorder insert <name> <index> : Insert an action at the chosen position",
            "/recorder append <name> : Shortcut to insert at the end",
            "/recorder help : How to use the recorder" // Explanation window, how to use recorder
        };

        /// <inheritdoc />
        public void OnCommand(GameClient client, string[] args)
        {
            if (client?.Player == null)
                return;

            if (!Properties.ENABLE_RECORDER)
            {
                client.Player.Out.SendMessage("The Recorder system is disabled.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }

            if (args.Length < 2)
            {
                SendUsage(client);
                return;
            }

            if (args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                SendHelpWindow(client);
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
                            client.Player.Out.SendMessage($"Unknown recorder '{name}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }
                    break;

                case "rename" when args.Length >= 4:
                    {
                        var oldName = args[2];
                        var newName = args[3];
                        if (newName.Length > Properties.RECORDER_MAX_NAME_LENGTH)
                        {
                            client.Player.Out.SendMessage($"Recorder name is too long (max {Properties.RECORDER_MAX_NAME_LENGTH} characters).", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }
                        if (RecorderMgr.RenameRecording(client.Player, oldName, newName))
                            client.Player.Out.SendMessage($"Recorder '{oldName}' renamed to '{newName}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        else
                            client.Player.Out.SendMessage($"Rename failed. '{oldName}' not found or '{newName}' is already in use.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }
                    break;

                case "icon" when args.Length >= 3:
                    {
                        var name = args[2];
                        if (RecorderMgr.SetRecorderIcon(client.Player, name))
                            client.Player.Out.SendMessage($"Your next spell will set the icon for [{name}].", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        else
                            client.Player.Out.SendMessage($"Unknown recorder '{name}'.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }
                    break;
                case "save" when args.Length >= 3:
                    RecorderMgr.StopAndSaveRecording(client.Player, args[2]);
                    break;
                case "import" when args.Length >= 4:
                    {
                        string sourceCharName = args[2];
                        string sourceRecorderName = args[3];
                        RecorderMgr.ImportRecorderAsync(client.Player, sourceCharName, sourceRecorderName);
                        client.Player.Out.SendMessage("Import request processing. This may take a moment...", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }
                    break;

                case "append" when args.Length >= 3:
                    RecorderMgr.StartAppendMode(client.Player, args[2]);
                    break;

                case "insert" when args.Length >= 4:
                    {
                        var name = args[2];
                        if (!int.TryParse(args[3], out int index) || index < 1)
                        {
                            client.Player.Out.SendMessage("Invalid index. Use a positive whole number.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }
                        RecorderMgr.StartInsertMode(client.Player, name, index);
                    }
                    break;

                case "discard" when args.Length >= 4:
                    {
                        var name = args[2];
                        if (!int.TryParse(args[3], out int index) || index < 1)
                        {
                            client.Player.Out.SendMessage("Invalid index. Use a positive whole number.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }
                        RecorderMgr.DiscardAction(client.Player, name, index);
                    }
                    break;

                case "list":
                    RecorderMgr.ListRecorders(client.Player);
                    break;

                default:
                    SendUsage(client);
                    break;
            }
        }

        /// <summary>
        /// Sends a short command list inline for quick reference.
        /// </summary>
        private static void SendUsage(GameClient client)
        {
            foreach (var line in UsageMessages)
                client.Player.Out.SendMessage(line, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Opens a detailed help window explaining the recorder system and all commands.
        /// </summary>
        private static void SendHelpWindow(GameClient client)
        {
            client.Player.Out.SendCustomTextWindow("Recorder Help", HelpLines);
        }
    }
}

