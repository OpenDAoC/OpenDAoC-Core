using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Scripts;

namespace Core.GS.Commands
{
	[Command(
		"&code",
		EPrivLevel.Admin,
		"AdminCommands.Code.Description",
		"AdminCommands.Code.Usage")]
	public class CodeCommand : ACommandHandler, ICommandHandler
	{

        private static log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void ExecuteCode(GameClient client, string methodBody)
        {
            var compiler = new CoreScriptCompiler();
            var compiledAssembly = compiler.CompileFromSource(GetCode(methodBody));

            var errorMessages = compiler.GetErrorMessages();
            if (errorMessages.Any())
            {
                if (client.Player != null)
                {
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Code.ErrorCompiling"), EChatType.CT_System, EChatLoc.CL_PopupWindow);

                    foreach (var errorMessage in errorMessages)
                        client.Out.SendMessage(errorMessage, EChatType.CT_System, EChatLoc.CL_PopupWindow);
                }
                else
                {
                    log.Debug("Error compiling code.");
                }
                return;
            }

            var methodinf = compiledAssembly.GetType("CodeCommand").GetMethod("DynMethod");

            try
            {
                methodinf.Invoke(null, new object[] { client.Player == null ? null : client.Player.TargetObject, client.Player });

                if (client.Player != null)
                {
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Code.CodeExecuted"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                }
                else
                {
                    log.Debug("Code Executed.");
                }

            }
            catch (Exception ex)
            {
                if (client.Player != null)
                {
                    string[] errors = ex.ToString().Split('\n');
                    foreach (string error in errors)
                        client.Out.SendMessage(error, EChatType.CT_System, EChatLoc.CL_PopupWindow);
                }
                else
                {
                    log.Debug("Error during execution.");
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
            text.Append("using Core;\n");
            text.Append("using Core.GS.AI;\n");
            text.Append("using Core.Database;\n");
            text.Append("using Core.GS;\n");
            text.Append("using Core.GS.Movement;\n");
            text.Append("using Core.GS.Expansions.Foundations;\n");
            text.Append("using Core.GS.Behaviors;\n");
            text.Append("using Core.GS.Calculators;\n");
            text.Append("using Core.GS.Crafting;\n");
            text.Append("using Core.GS.ECS;\n");
            text.Append("using Core.GS.Effects;\n");
            text.Append("using Core.GS.Keeps;\n");
            text.Append("using Core.GS.Quests;\n");
            text.Append("using Core.GS.Commands;\n");
            text.Append("using Core.GS.Scripts;\n");
            text.Append("using Core.GS.Scripts.Custom;\n");
            text.Append("using Core.GS.Packets;\n");
            text.Append("using Core.GS.Events;\n");
            text.Append("using Core.GS.GameUtils;\n");
            text.Append("using Core.GS.Players;\n");
            text.Append("using Core.GS.RealmAbilities;\n");
            text.Append("using Core.GS.Server;\n");
            text.Append("using Core.GS.Skills;\n");
            text.Append("using Core.GS.Spells;\n");
            text.Append("using Core.GS.Styles;\n");
            text.Append("using Core.GS.World;\n");
            text.Append("using log4net;\n");
            text.Append("public class CodeCommand {\n");
            text.Append("private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);\n");
            text.Append("public static GameClient Client = null;\n");
            text.Append("public static void print(object obj) {\n");
            text.Append("	string str = (obj==null)?\"(null)\":obj.ToString();\n");
            text.Append("	if (Client==null || Client.Player==null) Log.Debug(str);\n	else Client.Out.SendMessage(str, eChatType.CT_System, eChatLoc.CL_SystemWindow);\n}\n");
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
