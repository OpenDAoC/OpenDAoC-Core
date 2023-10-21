using System.Linq;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.ServerProperties;

namespace Core.GS.Commands;

[Command("&language", EPrivLevel.Player, "Change your language.",
    "Use '/language current' to see your current used language.",
    "Use '/language set [language]' to set your language.",
    "Use '/language show' to show all available languages and to see your current used language."
)]
public class LanguageCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "language"))
            return;

        if (client.Account.PrivLevel == (uint)EPrivLevel.Player &&
            !Properties.ALLOW_CHANGE_LANGUAGE)
        {
            DisplayMessage(client, "This server does not support changing languages.");
            return;
        }

        if (args.Length < 2)
        {
            DisplaySyntax(client);
            return;
        }

        switch (args[1].ToLower())
        {
            #region current

            case "current":
            {
                DisplayMessage(client,
                    LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Language.Current"),
                    client.Account.Language);
                return;
            }

            #endregion current

            #region set

            case "set":
            {
                if (args.Length < 3)
                {
                    DisplaySyntax(client, "set");
                    return;
                }

                if (!LanguageMgr.Languages.Contains(args[2].ToUpper()))
                {
                    DisplayMessage(client,
                        LanguageMgr.GetTranslation(client.Account.Language,
                            "Scripts.Players.Language.LanguageNotSupported", args[2].ToUpper()));
                    return;
                }

                client.Account.Language = args[2];
                GameServer.Database.SaveObject(client.Account);
                DisplayMessage(client,
                    LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Language.Set",
                        args[2].ToUpper()));
                return;
            }

            #endregion set

            #region show

            case "show":
            {
                string languages = "";
                foreach (string language in LanguageMgr.Languages)
                {
                    if (client.Account.Language == language)
                        languages +=
                            ("*" + language + ","); // The * marks a language as the players current used language
                    else
                        languages += (language + ",");
                }

                if (languages.EndsWith(","))
                    languages = languages.Substring(0, languages.Length - 1);

                DisplayMessage(client,
                    LanguageMgr.GetTranslation(client.Account.Language,
                        "Scripts.Players.Language.AvailableLanguages", languages));
                return;
            }

            #endregion show

            default:
            {
                DisplaySyntax(client);
                return;
            }
        }
    }
}