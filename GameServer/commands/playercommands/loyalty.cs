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
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

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
            
            List<AccountXRealmLoyalty> realmLoyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(client.Account.ObjectId)));
            int midLoyalty = 0;
            int hibLoyalty = 0;
            int albLoyalty = 0;

            foreach (var realm in realmLoyalty)
            {
                if (realm.Realm == (int) eRealm.Albion)
                    albLoyalty = realm.LoyalDays;
                if (realm.Realm == (int) eRealm.Hibernia)
                    hibLoyalty = realm.LoyalDays;
                if (realm.Realm == (int) eRealm.Midgard)
                    midLoyalty = realm.LoyalDays;
            }

            double albPercent = albLoyalty > 30 ? 30 / 30.0 : albLoyalty/30.0;
            double hibPercent = hibLoyalty > 30 ? 30/30.0 : hibLoyalty / 30.0;
            double midPercent = midLoyalty > 30 ? 30/30.0 : midLoyalty/ 30.0;

            DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
            lastUpdatedTime.AddDays(1);
            long timeMilli = (long)(lastUpdatedTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
            
            DisplayMessage(client, $"Alb Loyalty: {albLoyalty} days {(albPercent*100).ToString("0.##")}% | Hib Loyalty: {hibLoyalty} days {(hibPercent*100).ToString("0.##")}% | Mid Loyalty: {midLoyalty} days {(midPercent*100).ToString("0.##")}%");
            DisplayMessage(client, "Time until next loyalty tick: " + TimeSpan.FromMilliseconds(timeMilli).Hours + "h " + TimeSpan.FromMilliseconds(timeMilli).Minutes + "m " + TimeSpan.FromMilliseconds(timeMilli).Seconds + "s");
        }
    }
}