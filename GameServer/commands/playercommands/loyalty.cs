/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&loyalty",
        ePrivLevel.Player,
        "display current realm loyalty levels",
        "/loyalty")]
    public class LoyaltyCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {

            var playerLoyalty = LoyaltyManager.GetPlayerLoyalty(client.Player);
            var midLoyalty = playerLoyalty.MidLoyaltyDays;
            var hibLoyalty = playerLoyalty.HibLoyaltyDays;
            var albLoyalty = playerLoyalty.AlbLoyaltyDays;
            var albPercent = playerLoyalty.AlbPercent;
            var midPercent = playerLoyalty.MidPercent;
            var hibPercent = playerLoyalty.HibPercent;
            
            LoyaltyManager.LoyaltyUpdateAddDays(client.Player, 1);
            var lastUpdatedTime = LoyaltyManager.GetLastLoyaltyUpdate(client.Player);
            
            var timeMilli = (long)(lastUpdatedTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
            
            DisplayMessage(client, $"Alb Loyalty: {albLoyalty} days {(albPercent*100).ToString("0.##")}% | Hib Loyalty: {hibLoyalty} days {(hibPercent*100).ToString("0.##")}% | Mid Loyalty: {midLoyalty} days {(midPercent*100).ToString("0.##")}%");
            DisplayMessage(client, "Time until next loyalty tick: " + TimeSpan.FromMilliseconds(timeMilli).Hours + "h " + TimeSpan.FromMilliseconds(timeMilli).Minutes + "m " + TimeSpan.FromMilliseconds(timeMilli).Seconds + "s");
        }
    }
}