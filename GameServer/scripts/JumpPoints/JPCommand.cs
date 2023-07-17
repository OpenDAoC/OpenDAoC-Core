using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [Command(
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
            
            DBJumpPoint p = GameServer.Database.SelectObjects<DBJumpPoint>(DB.Column("Name").IsEqualTo(args[2])).FirstOrDefault();
            
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

            DBJumpPoint p = GameServer.Database.SelectObjects<DBJumpPoint>(DB.Column("Name").IsEqualTo(args[2])).FirstOrDefault();

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
                DBJumpPoint p = GameServer.Database.SelectObjects<DBJumpPoint>(DB.Column("Name").IsEqualTo(args[3])).FirstOrDefault();

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