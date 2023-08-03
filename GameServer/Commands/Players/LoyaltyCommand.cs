
using System;
using Core.GS.Players.Loyalty;

namespace DOL.GS.Commands
{
    [Command(
        "&loyalty",
        EPrivLevel.Player,
        "display current realm loyalty levels",
        "/loyalty")]
    public class LoyaltyCommand : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {

            var playerLoyalty = LoyaltyMgr.GetPlayerLoyalty(client.Player);
            var midLoyalty = playerLoyalty.MidLoyaltyDays;
            var hibLoyalty = playerLoyalty.HibLoyaltyDays;
            var albLoyalty = playerLoyalty.AlbLoyaltyDays;
            var albPercent = playerLoyalty.AlbPercent;
            var midPercent = playerLoyalty.MidPercent;
            var hibPercent = playerLoyalty.HibPercent;
            
            LoyaltyMgr.LoyaltyUpdateAddDays(client.Player, 1);
            var lastUpdatedTime = LoyaltyMgr.GetLastLoyaltyUpdate(client.Player);
            
            var timeMilli = (long)(lastUpdatedTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
            
            DisplayMessage(client, $"Alb Loyalty: {albLoyalty} days {(albPercent*100).ToString("0.##")}% | Hib Loyalty: {hibLoyalty} days {(hibPercent*100).ToString("0.##")}% | Mid Loyalty: {midLoyalty} days {(midPercent*100).ToString("0.##")}%");
            DisplayMessage(client, "Time until next loyalty tick: " + TimeSpan.FromMilliseconds(timeMilli).Hours + "h " + TimeSpan.FromMilliseconds(timeMilli).Minutes + "m " + TimeSpan.FromMilliseconds(timeMilli).Seconds + "s");
        }
    }
}