using System.Reflection;
using System.Text;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.Language;
using log4net;

namespace Core.GS.Commands;

[Command(
    "&battlechat",
    new string[] { "&bc", "&bchat" },
    EPrivLevel.Player,
    "Battle group chat command",
    "/bc <text>")]
public class BattleGroupChatCommand : ACommandHandler, ICommandHandler
{
    protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "battlechat"))
            return;

        BattleGroupUtil mybattlegroup = client.Player.TempProperties.GetProperty<BattleGroupUtil>(BattleGroupUtil.BATTLEGROUP_PROPERTY, null);
        if (mybattlegroup == null)
        {
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
        }

        var isLeader = mybattlegroup.IsBGLeader(client.Player);
        var isModerator = mybattlegroup.IsBGModerator(client.Player);
        if (mybattlegroup.Listen && !isLeader && !isModerator)
        {
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.OnlyModerator"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
        }
        if (args.Length < 2)
        {
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Usage"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
        }

        StringBuilder text = new StringBuilder(7 + 3 + client.Player.Name.Length + (args.Length - 1) * 8);

        text.Append("[BattleGroup] ");
        text.Append(client.Player.Name);
        text.Append(": \"");
        text.Append(args[1]);
        for (int i = 2; i < args.Length; i++)
        {
            text.Append(" ");
            text.Append(args[i]);
        }
        text.Append("\"");
        var message = text.ToString();
        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
        {
            EChatType type;

            if (isLeader || isModerator)
            {
                type = EChatType.CT_BattleGroupLeader;
            }
            else
            {
                type = EChatType.CT_BattleGroup;
            }

            if (ply.IgnoreList.Contains(client.Player)) continue;
            
            ply.Out.SendMessage(message,type, EChatLoc.CL_ChatWindow);
        }
    }
}