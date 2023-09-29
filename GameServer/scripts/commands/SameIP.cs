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
            List<string> output = new();
            Hashtable ip = new();
            string accountIp;

            foreach (GameClient otherClient in ClientService.GetClients())
            {
                if (otherClient.Account.PrivLevel > 1)
                    continue;

                accountIp = ((IPEndPoint) otherClient.Socket.RemoteEndPoint).Address.ToString();

                if (!ip.Contains(accountIp))
                    ip.Add(accountIp, otherClient);
                else
                {
                    GameClient cls = (GameClient)ip[accountIp];

                    string name1 = otherClient.Player != null ? otherClient.Player.Name : "Entering game...";
                    string ip1 = ((IPEndPoint)otherClient.Socket.RemoteEndPoint).Address.ToString();
                    string name2 = cls.Player != null ? cls.Player.Name : "Entering game...";
                    string ip2 = ((IPEndPoint)cls.Socket.RemoteEndPoint).Address.ToString();

                    output.Add($"Same IP violation #{i} - IP: {accountIp}");
                    output.Add($"Acc 1: {otherClient.Account.Name} ({name1}  L{otherClient.Player?.Level} {otherClient.Player?.CharacterClass.Name} in {otherClient.Player?.CurrentZone.Description})");
                    output.Add($"Acc 2: {cls.Account.Name} ({name2} L{cls.Player?.Level} {cls.Player?.CharacterClass.Name} in {cls.Player?.CurrentZone.Description})");
                    output.Add("\n");
                    i++;
                }
            }

            DisplayMessage(client, $"{i - 1} double IP found.");

            if (i - 1 > 0)
                client.Out.SendCustomTextWindow($"{i - 1} double IP found", output);
        }
    }
}
