using System;
using DOL.Database;

namespace DOL.GS
{
    public static class ClientExtensions
    {
        public static void BanAccount(this GameClient client, string reason)
        {
            DbBans b = new()
            {
                Author = "SERVER",
                Ip = client.TcpEndpointAddress,
                Account = client.Account.Name,
                DateBan = DateTime.Now,
                Type = "B",
                Reason = reason
            };

            GameServer.Database.AddObject(b);
            GameServer.Instance.LogCheatAction($"{b.Reason}. Client Account: {client.Account.Name}");
        }
    }
}
