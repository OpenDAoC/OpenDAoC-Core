using System;
using System.Text;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    // Adding the ampersand and inheritance from AbstractCommandHandler
    [CmdAttribute("&bm", ePrivLevel.Player, "Sends a screen center message to all battlegroup members", "/bm <text>")]
    public class BattlegroupMessageCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            if (player == null)
                return;

            BattleGroup bg = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
            if (bg == null)
            {
                player.Out.SendMessage("You must be in a battlegroup to use this command.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            bool isLeader = bg.IsBGLeader(player);
            bool isModerator = bg.IsBGModerator(player);

            if (!isLeader && !isModerator)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.OnlyModerator"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (args.Length < 2) // Note: With AbstractCommandHandler, args[0] is often the command name
            {
                player.Out.SendMessage("Usage: /bm <text>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // Combine the message, skipping the first argument (the command itself)
            string message = string.Join(" ", args, 1, args.Length - 1);

            foreach (GamePlayer ply in bg.Members.Keys)
            {
                // We send it as Screen Center, but also usually to the Chat Window so it isn't missed
                ply.Out.SendMessage($"{message}", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            }
        }
    }
}