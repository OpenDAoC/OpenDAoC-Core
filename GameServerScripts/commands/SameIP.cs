using System;
using DOL.GS;
using DOL.Database;
using DOL.GS.PacketHandler;
using System.Net;
using System.Collections;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		 "&sameip",
		 ePrivLevel.GM,
		 "Find the double logins",
		 "/sameip")]
	public class DoubleIPCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			int i = 0;
			Hashtable ip = new Hashtable();
			string accip;
			foreach (GameClient cl in WorldMgr.GetAllClients())
			{
				accip = ((IPEndPoint)cl.Socket.RemoteEndPoint).Address.ToString();

				if (!ip.Contains(accip))
					ip.Add(accip, cl);
				else
				{
					GameClient cls = (GameClient)ip[accip];

					string name1 = cl.Player != null ? cl.Player.Name : "Entering game...";
					string ip1 = ((IPEndPoint)cl.Socket.RemoteEndPoint).Address.ToString();
					string name2 = cls.Player != null ? cls.Player.Name : "Entering game...";
					string ip2 = ((IPEndPoint)cls.Socket.RemoteEndPoint).Address.ToString();

					DisplayMessage(client, "IP: {0} AccountName: {1} (Player: {2}) ", ip1, cl.Account.Name, name1);
					DisplayMessage(client, "IP: {0} AccountName: {1} (Player: {2}) ", ip2, cls.Account.Name, name2);
					i++;
				}
			}
            //DisplayMessage.Out.SendMessage(client, "{0} double IP found.", i, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			DisplayMessage(client, "{0} double IP found.", i);			
		}
	}
}
