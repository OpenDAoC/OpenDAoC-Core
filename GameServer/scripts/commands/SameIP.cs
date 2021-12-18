using System.Net;
using System.Collections;
using System.Collections.Generic;

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
			int i = 1;
			IList<string> output = new List<string>();
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
					
					output.Add("Same IP violation #" + i);
					output.Add("IP: " + ip1);
					output.Add("Account 1: " + cl.Account.Name);
					output.Add("Player 1: " + name1 + " (L" + cl.Player?.Level + " " + cl.Player?.CharacterClass.Name + " in " + cl.Player?.CurrentZone.Description + ")");
					output.Add("X: " + cl.Player?.X + " Y: " + cl.Player?.Y + " Z: " + cl.Player?.Z + " Region: " + cl.Player?.CurrentRegionID);
					output.Add("Account 2: " + cls.Account.Name);
					output.Add("Player 2: " + name2 + " (L" + cls.Player?.Level + " " + cls.Player?.CharacterClass.Name + " in " + cls.Player?.CurrentZone.Description + ")");
					output.Add("X: " + cls.Player?.X + " Y: " + cls.Player?.Y + " Z: " + cls.Player?.Z + " Region: " + cls.Player?.CurrentRegionID);
					output.Add("\n");
					i++;
				}
			}
			DisplayMessage(client, "{0} double IP found.", i-1);
			if (i - 1 > 0)
			{
				client.Out.SendCustomTextWindow(i-1 + " double IP found", output);
			}
		}
	}
}
