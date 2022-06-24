using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.PlayerTitles
{
    /// <summary>
    /// Example...
    /// </summary>
    ///
    public class LaunchQuestTitle : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Congrats, I guess?";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Congrats, I guess?";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Launch Quest title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return AchievementUtils.CheckAccountCredit("LaunchQuest", player);;
        }
    }
    
    public class LaunchDayTitle : SimplePlayerTitle
    {
        public override string GetDescription(GamePlayer player)
        {
            return "The Patient";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "The Patient";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Launch Day title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            var launch = new DateTime(2022, 06, 25, 00, 00, 00);
            var creationDate = player.DBCharacter.CreationDate;
            
            return (creationDate < launch);
        }
    }
}