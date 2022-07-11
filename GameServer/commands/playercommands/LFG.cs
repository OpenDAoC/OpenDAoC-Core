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

using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;
using DOL.GS.ServerProperties;
using DOL.Language;


namespace DOL.GS.Commands
{
    [CmdAttribute(
         "&lfg",
         ePrivLevel.Player,
         "Broadcast a LFG message to other players in the same region",
         "/lfg <message>")]
    public class LFGCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const string lfgTimeoutString = "lastLFGTick";
        public void OnCommand(GameClient client, string[] args)
        {

            if (args.Length < 2)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Broadcast.NoText"));
                return;
            }
            if (client.Player.IsMuted)
            {
                client.Player.Out.SendMessage("You have been muted. You cannot broadcast.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                return;
            }
            string message = string.Join(" ", args, 1, args.Length - 1);

            var lastLFGTick = client.Player.TempProperties.getProperty<long>(lfgTimeoutString);
            var slowModeLength = Properties.LFG_SLOWMODE_LENGTH * 1000;
			
            if ((GameLoop.GameLoopTime - lastLFGTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
            {
                // Message: You must wait {0} seconds before using this command again.
                ChatUtil.SendSystemMessage(client, "PLCommands.LFG.List.Wait", Properties.LFG_SLOWMODE_LENGTH - (GameLoop.GameLoopTime - lastLFGTick) / 1000);
                return;
            }
            
            Broadcast(client.Player, message);
        }

        private void Broadcast(GamePlayer player, string message)
        {
            foreach (GameClient c in WorldMgr.GetAllPlayingClients())
            {
                if (player.Realm == c.Player.Realm || c.Account.PrivLevel > 1)
                {
                    if (c.Player.SerializedIgnoreList.Contains(player.Name)) continue;
                    c.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.LFG.Message", player.Name + " (" + player.Level + ", " + player.CharacterClass.Name + ")", message), eChatType.CT_LFG, eChatLoc.CL_ChatWindow);
                }
            }

            if (Properties.DISCORD_ACTIVE) WebhookMessage.LogChatMessage(player, eChatType.CT_LFG, message);
            
            if (player.Client.Account.PrivLevel == 1)
            {
                player.Client.Player.TempProperties.setProperty(lfgTimeoutString, GameLoop.GameLoopTime);
            }   
        }

    }
}


