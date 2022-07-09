using System.Collections.Generic;
using System.Reflection;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
    [Cmd(
        "&achievement",
        ePrivLevel.Admin,
        "/achievement add <MobName> ie: /achievement add Organic-Energy Mechanism",
        "/achievement addbg <MobName> ie: /achievement addbg Olcasgean",
        "/achievement addgroup <MobName> ie: /achievement addgroup Cuuldurach the Glimmer King")]
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
                    client.Player.Out.SendMessage("You need a target to add an achievement.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                if (client.Player.TargetObject is not GamePlayer targetPlayer)
                {
                    client.Player.Out.SendMessage("You need a player target to add an achievement.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                    client.Player.Out.SendMessage("You need to join the target battlegroup to add an achievement.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                    client.Player.Out.SendMessage("You need to join the target group to add an achievement.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                    player.Out.SendMessage("Error loading the temp properties", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                foreach (var p in targetPlayers)
                {
                    var hascredit = AchievementUtils.CheckPlayerCredit(achievementName, p, (int) p.Realm);
                    if (hascredit) continue;

                    var credit = achievementName + "-Credit";
                    p.Achieve(credit);
                    log.Warn($"ACHIEVEMENT: Manually added credit for {achievementName} to player {p.Name} ({player.Name})");
                }
                
                player.Out.SendMessage($"Achievement {achievementName} added to the selected player(s)", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
            else
            {
                player.Out.SendMessage("Use the command again if you change your mind.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
            
            player.TempProperties.removeProperty("AchievementName");
            player.TempProperties.removeProperty("targetPlayers");
        }
    }
}