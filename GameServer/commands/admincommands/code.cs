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
using System.Reflection;
using System.Text;

namespace DOL.GS.Commands
{
    /// <summary>
    /// Handles all user-based interaction for the '/code' command
    /// </summary>
	[CmdAttribute(
        // Enter '/code' to list all associated subcommands
        "&code",
        // Message: '/code' - Manually executes a custom script in-game.
        "AdminCommands.Code.CmdList.Description",
        // Message: <----- '/{0}' Command {1}----->
        "AllCommands.Header.General.Commands",
        // Required minimum privilege level to use the command
        ePrivLevel.Admin,
        // Message: Manually executes a custom script in-game.
        "AdminCommands.Code.Description",
        // Syntax: /code <className>
        "AdminCommands.Code.Syntax.Code",
        // Message: Triggers the system compiler for the specified script.
        "AdminCommands.Code.Usage.Code"
    )]
	public class DynCodeCommandHandler : AbstractCommandHandler, ICommandHandler
	{

        private static log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void ExecuteCode(GameClient client, string methodBody)
        {
            var compiler = new DOLScriptCompiler();
            var compiledAssembly = compiler.CompileFromSource(GetCode(methodBody));

            var errorMessages = compiler.GetErrorMessages();
            if (errorMessages.Any())
            {
                if (client.Player != null)
                {
                    // Message: Error(s) occurred while compiling the specified code:
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Code.Err.Compiling", null);

                    // Send each error message that occurs
                    foreach (var errorMessage in errorMessages)
                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, errorMessage);
                }
                else
                {
                    log.Debug("[FAILED] - An error occurred while compiling for the '/code' command.");
                }
                return;
            }

            var methodinf = compiledAssembly.GetType("DynCode").GetMethod("DynMethod");

            try
            {
                methodinf.Invoke(null, new object[] { client.Player == null ? null : client.Player.TargetObject, client.Player });

                if (client.Player != null)
                {
                    // Message: The specified code executed successfully!
                    ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Code.Msg.CodeExecuted", null);
                }
                else
                {
                    log.Debug("[SUCCESS] - Code executed using the '/code' command.");
                }

            }
            catch (Exception ex)
            {
                if (client.Player != null)
                {
                    string[] errors = ex.ToString().Split('\n');
                    foreach (string error in errors)
                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, error, null);
                }
                else
                {
                    log.Debug("[FAILED] - An unexpected error occurred while attempting to execute the '/code' command.");
                }
            }
        }

        private static string GetCode(string methodBody)
        {
            StringBuilder text = new StringBuilder();
            text.Append("using System;\n");
            text.Append("using System.Reflection;\n");
            text.Append("using System.Collections;\n");
            text.Append("using System.Threading;\n");
            text.Append("using DOL;\n");
            text.Append("using DOL.AI;\n");
            text.Append("using DOL.AI.Brain;\n");
            text.Append("using DOL.Database;\n");
            text.Append("using DOL.GS;\n");
            text.Append("using DOL.GS.Movement;\n");
            text.Append("using DOL.GS.Housing;\n");
            text.Append("using DOL.GS.Keeps;\n");
            text.Append("using DOL.GS.Quests;\n");
            text.Append("using DOL.GS.Commands;\n");
            text.Append("using DOL.GS.Scripts;\n");
            text.Append("using DOL.GS.PacketHandler;\n");
            text.Append("using DOL.Events;\n");
            text.Append("using log4net;\n");
            text.Append("public class DynCode {\n");
            text.Append("private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);\n");
            text.Append("public static GameClient Client = null;\n");
            text.Append("public static void print(object obj) {\n");
            text.Append("	string str = (obj==null)?\"(null)\":obj.ToString();\n");
            text.Append("	if (Client==null || Client.Player==null) Log.Debug(str);\n	else ChatUtil.SendTypeMessage((int)eMsg.Error, client, str);\n}\n");
            text.Append("public static void DynMethod(GameObject target, GamePlayer player) {\nif (player!=null) Client = player.Client;\n");
            text.Append("GameNPC targetNpc = target as GameNPC;");
            text.Append(methodBody);
            text.Append("\n}\n}\n");
            return text.ToString();
        }

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }
            string code = String.Join(" ", args, 1, args.Length - 1);
            ExecuteCode(client, code);
        }
    }
}
