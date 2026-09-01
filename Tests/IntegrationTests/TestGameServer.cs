using System;
using System.IO;
using DOL.Database.Connection;

namespace DOL.GS.Tests.Integration
{
    public class TestGameServer : GameServer
    {
        private TestGameServer(GameServerConfiguration config) : base(config) { }

        public static TestGameServer Install()
        {
            if (Instance is TestGameServer existing)
                return existing;

            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
            string dbPath = Path.Combine(root, "dol-tests-only.sqlite3.db");
            File.Delete(dbPath);

            GameServerConfiguration config = new()
            {
                RootDirectory = root,
                DBType = EConnectionType.DATABASE_SQLITE,
                DBConnectionString = $"Data Source={dbPath};Version=3;Pooling=False;Cache Size=1073741824;Journal Mode=Off;Synchronous=Off;Foreign Keys=True;Default Timeout=60"
            };

            TestGameServer server = new(config);
            m_instance = server;
            server.NpcManager = new NpcManager(server);
            return server;
        }
    }
}
