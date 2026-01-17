using System;
using System.Text;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute("&bm", ePrivLevel.Player, "Sends a screen center message to all battlegroup members", "/bm <text>")]
    public class BattlegroupMessageCommandHandler : ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            if (player == null)
                return;

            BattleGroup bg = client.Player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
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

            // 3. Textprüfung
            if (args.Length < 1)
            {
                player.Out.SendMessage("Usage: /bm <text>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 4. Text zusammenbauen
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) sb.Append(" ");
                sb.Append(args[i]);
            }
            string message = sb.ToString();

            foreach (GamePlayer ply in bg.Members.Keys)
            {
                ply.Out.SendMessage(message, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            }
        }
    }
}