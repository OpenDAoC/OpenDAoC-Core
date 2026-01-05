using DOL.GS.PacketHandler;
using DOL.GS; 
using DOL.GS.Quests; // WICHTIG: Für den Zugriff auf AbstractQuest
// using DOL.Language; // Wird hier nicht direkt benötigt, aber schadet nicht

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&clearfinishedquests",
        ePrivLevel.GM, // NUR Admins
        "Deletes all completed quest entries from the character's finished list and the database.",
        "/clearquests [confirm]")]
    public class ClearFinishedQuestsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            if (IsSpammingCommand(player, "clearquests"))
                return;

            // Führt eine Sicherheitsabfrage ein
            if (args.Length < 2 || args[1].ToLower() != "confirm")
            {
                client.Out.SendMessage(
                    "WARNING: This command will permanently delete all finished quest records for your character from the database.",
                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                
                client.Out.SendMessage(
                    "To proceed, type: /clearquests confirm",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // Ruft die statische Methode aus AbstractQuest auf
            AbstractQuest.ClearFinishedQuests(player); 
        }
    }
}