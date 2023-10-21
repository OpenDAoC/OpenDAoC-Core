using System;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.PlayerTitles
{
    /// <summary>
    /// Example...
    /// </summary>
    ///
    public class LaunchQuestTitle : APlayerTitle
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
            player.Out.SendMessage("You have gained the Launch Quest title!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return AchievementUtil.CheckAccountCredit("LaunchQuest", player);;
        }
    }
    
    public class LaunchDayTitle : APlayerTitle
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
            player.Out.SendMessage("You have gained the Launch Day title!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            var launch = new DateTime(2022, 06, 27, 00, 00, 00);
            var creationDate = player.DBCharacter.CreationDate;
            
            return (creationDate < launch);
        }
    }
}