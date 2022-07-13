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
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&classrog",
        ePrivLevel.Player,
        "change the chance% of getting ROGs outside of your current class at level 50," +
        " or the likelihood of getting items relevant to your spec while under 50",
        "/classrog <%chance>")]
    public class ClassRogsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            
            AccountXRealmLoyalty realmLoyalty = DOLDB<AccountXRealmLoyalty>.SelectObject(DB.Column("AccountID").IsEqualTo(client.Account.ObjectId).And(DB.Column("Realm").IsEqualTo(client.Player.Realm)));
            int ROGCap = 0;
            int tmpLoyal = realmLoyalty.LoyalDays > 30 ? 30 : realmLoyalty.LoyalDays;
            int cachedInput = 0;
            if (realmLoyalty != null)
            {
                //max cap of 50% out of class chance
                //scaled by loyalty%
                ROGCap = (int)Math.Round(50 * (tmpLoyal / 30.0)); 
            }
            
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                DisplayMessage(client, "Current cap: " + ROGCap);
                return;
            }

            cachedInput = int.Parse(args[1]);
            if ( cachedInput > ROGCap)
            {
                DisplayMessage(client, "Input too high. Defaulting to cap: " + ROGCap);
                cachedInput = ROGCap;
            }
            else if (cachedInput < 0)
            {
                DisplayMessage(client, "Input must be 0 or above. Current cap: " + ROGCap);
                return;
            }

            client.Player.OutOfClassROGPercent = cachedInput;
            
            if(client.Player.Level == 50)
                DisplayMessage(client, "You will now receive out of class ROGs " + args[1] + "% of the time.");
            else
            {
                //DisplayMessage(client, "You are now " + client.Player.OutOfClassROGPercent + "% more likely to get ROGs relevant to your spec.");
            }
        }
    }
}