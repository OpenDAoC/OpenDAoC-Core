using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DOL.GameServerConsole;
using DOL.GS;
using DOL.GS.ServerProperties;

namespace DOL.DOLServer.Actions
{
    /// <summary>
    /// Handles console start requests of the gameserver
    /// </summary>
    public class ConsoleStart : IAction
    {
        /// <summary>
        /// returns the name of this action
        /// </summary>
        public string Name
        {
            get { return "--start"; }
        }

        /// <summary>
        /// returns the syntax of this action
        /// </summary>
        public string Syntax
        {
            get { return "--start [-config=./config/serverconfig.xml]"; }
        }

        /// <summary>
        /// returns the description of this action
        /// </summary>
        public string Description
        {
            get { return "Starts the DOL server in console mode"; }
        }

        /// <summary>
        /// Mock client for console commands
        /// </summary>
        private GameClient _consoleClient;

        private bool _crashOnFail = false;

        private static bool StartServer()
        {
            Console.WriteLine("Starting GameServer");
            bool start = GameServer.Instance.Start();
            return start;
        }

        public void OnAction(Hashtable parameters)
        {
            Console.WriteLine("Starting...");
            FileInfo configFile;
            FileInfo currentAssembly = null;
            if (parameters["-config"] != null)
            {
                Console.WriteLine("Using config file: " + parameters["-config"]);
                configFile = new FileInfo((String) parameters["-config"]);
            }
            else
            {
                currentAssembly = new FileInfo(Assembly.GetEntryAssembly().Location);
                configFile = new FileInfo(currentAssembly.DirectoryName + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "serverconfig.xml");
            }
            if (parameters.ContainsKey("-crashonfail"))
                _crashOnFail = true;

            var config = new GameServerConfiguration();
            if (configFile.Exists)
            {
                config.LoadFromXMLFile(configFile);
            }
            else
            {
                if (!configFile.Directory.Exists)
                    configFile.Directory.Create();
                config.SaveToXMLFile(configFile);
                if (File.Exists(currentAssembly.DirectoryName + Path.DirectorySeparatorChar + "DOLConfig.exe"))
                {
                    Console.WriteLine("No config file found, launching with default config and embedded database... (SQLite)");
                }
            }

            GameServer.CreateInstance(config);
            StartServer();

            if (_crashOnFail && GameServer.Instance.ServerStatus == EGameServerStatus.GSS_Closed)
            {
                throw new ApplicationException("Server did not start properly.");
            }

            bool run = true;

            while (run)
            {
                string line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                    continue;

                switch (line.ToLower())
                {
                    case "exit":
                        run = false;
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    default:
                        ProcessCommand(line);
                        break;
                }
            }

            GameServer.Instance?.Stop();
        }

        private void ProcessCommand(string line)
        {
            try
            {
                if (line[0] != '/')
                    line = $"/{line}";

                EnsureConsoleClientIsInitialized();

                if (!ScriptMgr.HandleCommand(_consoleClient, $"&{line[1..]}"))
                {
                    ConsoleColor before = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Unknown command: {line}");
                    Console.ForegroundColor = before;
                }
            }
            catch (Exception e)
            {
                ConsoleColor before = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(e.ToString());
                Console.ForegroundColor = before;
            }
        }

        private void EnsureConsoleClientIsInitialized()
        {
            if (_consoleClient != null)
                return;

            _consoleClient = new(null)
            {
                Account = new()
                {
                    Name = "ConsoleAdmin",
                    Language = Properties.SERV_LANGUAGE,
                    PrivLevel = (uint) ePrivLevel.Admin
                },
                ClientState = GameClient.eClientState.Playing,
                Out = new ConsolePacketLib()
            };
            _consoleClient.Player = new(_consoleClient, null)
            {
                Name = _consoleClient.Account.Name,
                Realm = eRealm.None
            };
        }
    }
}
