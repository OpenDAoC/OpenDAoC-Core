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
 * Written by Biceps (thebiceps@gmail.com)
 * Distributed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 license
 * http://creativecommons.org/licenses/by-nc-sa/3.0/
 * 
 * Added: 14:48 2007-07-04
 * Last updated: 22:32 2017-05
 * 
 * Updated by Unty for latest DOL revisions
 */

using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
		"&jp",
        ePrivLevel.GM,
        "Modify or use the jumppoint system",
        //usage
        "/jp add <name>",
        "/jp list",
        "/jp port to <name>",
        "/jp remove <name>")]
    public class OnJumpPoint : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
        	if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
        	switch(args[1])
            {
                case "add":
            		AddJumpPoint(client, args); break;
                case "list":
                    ListJumpPoints(client); break;
                case "port":
                    PortToJumpPoint(client,args); break;
                case "remove":
                    RemoveJumpPoint(client, args); break;
                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        private void AddJumpPoint(GameClient client, string[] args)
        {
            if (args.Length != 3)
            {
                client.Out.SendMessage("Usage : /jp add <name>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            DBJumpPoint p = GameServer.Database.SelectObjects<DBJumpPoint>("Name = @Name", new QueryParameter("@Name", args[2])).FirstOrDefault();
            
            if(p != null)
            {
                SendSystemMessage(client, "JumpPoint with name '" + args[2] + "' already exists");
                return;
            }
            
            p = new DBJumpPoint();
            p.Xpos = client.Player.X;
            p.Ypos = client.Player.Y;
            p.Zpos = client.Player.Z;
            p.Region = client.Player.CurrentRegionID;
            p.Heading = client.Player.Heading;
            p.Name = args[2];

            GameServer.Database.AddObject(p);
            
            SendSystemMessage(client,"JumpPoint added with name '" + args[2] + "'");            
        }

        private void ListJumpPoints(GameClient client)
        {
        	var col = GameServer.Database.SelectAllObjects<DBJumpPoint>();
            
            SendSystemMessage(client,"----------List of JumpPoints----------");

            foreach (DBJumpPoint p in col)
            {
                SendSystemMessage(client, p.Name);
            }            
        }

        private void RemoveJumpPoint(GameClient client, string[] args)
        {
            if (args.Length != 3)
            {
                SendSystemMessage(client, "Usage : /jp remove <name>");
                return;
            }

            DBJumpPoint p = GameServer.Database.SelectObjects<DBJumpPoint>("Name = @Name", new QueryParameter("@Name", args[2])).FirstOrDefault();;

            if(p == null)
            {
                SendSystemMessage(client, "No JumpPoint with name '" + args[2] + "' found");
                return;
            }
            
            GameServer.Database.DeleteObject(p);
            SendSystemMessage(client, "Removed JumpPoint with name '" + args[2] + "'");            
        }

        private void PortToJumpPoint(GameClient client, string[] args)
        {
            if (args.Length == 4 && args[2] == "to")
            {
                DBJumpPoint p = GameServer.Database.SelectObjects<DBJumpPoint>("Name = @Name", new QueryParameter("@Name", args[3])).FirstOrDefault();

                if(p == null)
                {
                    SendSystemMessage(client, "No JumpPoint with name '" + args[3] + "' found");
                    return;
                }
                if (CheckExpansion(client, client, p.Region))
                {
                	client.Player.MoveTo(p.Region, p.Xpos, p.Ypos, p.Zpos, p.Heading);
                }                
            }            
            else
            {
                SendSystemMessage(client, "Usage : /jp port to <name>");
                return;
            }
        }

        private void SendSystemMessage(GameClient client, string msg)
        {
            client.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private bool CheckExpansion(GameClient clientJumper, GameClient clientJumpee, ushort RegionID)
        {
            Region reg = WorldMgr.GetRegion(RegionID);
            
            if (reg != null && reg.Expansion > (int)clientJumpee.ClientType)
            {
                clientJumper.Out.SendMessage(clientJumpee.Player.Name + " cannot jump to Destination region (" + reg.Description + ") because it is not supported by your client type.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            return true;
        }
    }
}