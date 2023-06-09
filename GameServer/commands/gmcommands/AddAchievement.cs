using System.Collections.Generic;
using System.Reflection;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
    [Cmd(
        // Enter '/achievement' to list all associated subcommands
        "&achievement",
        // Message: '/achievement' - Grants boss kill credit to an individual player, group, or battlegroup.
        "GMCommands.CmdList.Achievement.Description",
        // Message: <----- '/{0}' Command {1}----->
        "AllCommands.Header.General.Commands",
        // Required minimum privilege level to use the command
        ePrivLevel.Admin,
        // Message: Allows server staff to manually grant kill credit for an individual boss mob to a player, group, or battlegroup.
        "GMCommands.Achievement.Description",
        // Message: /achievement add <mobName>
        "GMCommands.Achievement.Syntax.SingleAdd",
        // Message: Grants kill credit to the player you are presently targeting.
        "GMCommands.Achievement.Usage.SingleAdd",
        // Message: /achievement addbg <mobName>
        "GMCommands.Achievement.Syntax.BGAdd",
        // Message: Grants kill credit to all members of the battlegroup you're in.
        "GMCommands.Achievement.Usage.BGAdd",
        // Message: /achievement addgroup <mobName>
        "GMCommands.Achievement.Syntax.GroupAdd",
        // Message: Grants kill credit to all members of the group you're in.
        "GMCommands.Achievement.Usage.GroupAdd")]
    public class AddAchievementCommandHandler : AbstractCommandHandler, ICommandHandler
    {        
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public void OnCommand(GameClient client, string[] args)
        {
            
            if (IsSpammingCommand(client.Player, "addachievement"))
                return;
            
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            var achievementName = "";
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }
            
            if (args.Length == 3)
            {
                achievementName = args[2];
            }
            else
            {
                for (var i = 2; i < args.Length; i++)
                {
                    achievementName += args[i] + " ";
                }
            }

            var targetPlayers = new List<GamePlayer>();
            
            var count = 0;

            if (args[1] == "add")
            {
                if (client.Player.TargetObject == null)
                {
                    // Message: [ERROR] You need to target a player to add an achievement.
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "GMCommands.Achievement.Err.TargetPlayer", null);
                    return;
                }
                
                if (client.Player.TargetObject is not GamePlayer targetPlayer)
                {
                    // Message: [ERROR] You need to target a player to add an achievement.
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "GMCommands.Achievement.Err.TargetPlayer", null);
                    return;
                }
                
                targetPlayers.Add(targetPlayer);
                count++;
            } 
            else if (args[1] == "addbg")
            {
                var bg = (BattleGroup)client.Player.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                if (bg == null)
                {
                    // Message: [ERROR] You must join a battlegroup to add an achievement for its members.
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "GMCommands.Achievement.Err.JoinBG", null);
                    return;
                }

                foreach (GamePlayer bgP in bg.Members.Keys)
                {
                    if (bgP.Name == client.Player.Name) continue; // skipping the command user
                    if (bgP.Client.Account.PrivLevel > 1) continue; // skipping GMs
                    
                    targetPlayers.Add(bgP);
                    count++;
                }
            }
            else if (args[1] == "addgroup")
            {
                if (client.Player.Group == null)
                {
                    // Message: [ERROR] You must join a group to add an achievement for its members.
                    ChatUtil.SendTypeMessage((int)eMsg.Error, client, "GMCommands.Achievement.Err.JoinGroup", null);
                    return;
                }
                
                var group = client.Player.Group;

                foreach (GamePlayer gP in group.GetPlayersInTheGroup())
                {
                    if (gP.Name == client.Player.Name) continue; // skipping the command user
                    if (gP.Client.Account.PrivLevel > 1) continue; // skipping GMs
                    
                    targetPlayers.Add(gP);
                    count++;
                }
            }
            else
            {
                DisplaySyntax(client);
                return;
            }
            
            // to remove
            client.Player.TempProperties.setProperty("AchievementName", achievementName);
            client.Player.TempProperties.setProperty("targetPlayers", targetPlayers);
            
            client.Out.SendCustomDialog($"Confirm adding the achievement \"{achievementName}\" to {count} player(s)?", AddAchievementResponseHandler);
        }
        
        protected virtual void AddAchievementResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                var targetPlayers = player.TempProperties.getProperty<List<GamePlayer>>("targetPlayers");
                var achievementName = player.TempProperties.getProperty<string>("AchievementName");

                if (targetPlayers == null || achievementName == null)
                {
                    // Message: [ERROR] An error occurred while loading TempProperties!
                    ChatUtil.SendTypeMessage((int)eMsg.Debug, player, "GMCommands.Achievement.Err.ErrorTempProp", null);
                    return;
                }

                foreach (var p in targetPlayers)
                {
                    var hascredit = AchievementUtils.CheckPlayerCredit(achievementName, p, (int) p.Realm);
                    
                    if (hascredit) continue;

                    var credit = achievementName + "-Credit";
                    p.Achieve(credit);
                    
                    // Message: {0} has given you achievement credit for {1}!
                    ChatUtil.SendTypeMessage((int)eMsg.Staff, p, "GMCommands.Achievement.Msg.YouGotAchievement", player.Name, achievementName);
                    
                    log.Warn($"[SUCCESS] - ACHIEVEMENT: Manually granted credit for {achievementName} to player {p.Name} ({player.Name}).");
                }
                
                // Message: [SUCCESS] An achievement for {0} has been added to the selected player(s)!
                ChatUtil.SendTypeMessage((int)eMsg.Important, player, "GMCommands.Achievement.Msg.AchievementAdded", achievementName);
            }
            else
            {
                // Message: [INFO] Achievement credit aborted. Use the command again if you change your mind.
                ChatUtil.SendTypeMessage((int)eMsg.Debug, player, "GMCommands.Achievement.Msg.AchievementAborted", null);
            }
            
            player.TempProperties.removeProperty("AchievementName");
            player.TempProperties.removeProperty("targetPlayers");
        }
    }
}