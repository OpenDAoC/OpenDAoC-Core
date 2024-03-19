using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DOL.GS;
using DOLGameServerConsole;
using log4net;

namespace DOL.DOLServer.Actions
{
	/// <summary>
	/// Handles console start requests of the gameserver
	/// </summary>
	public class ConsoleStart : IAction
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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

		private bool crashOnFail = false;


		private static bool StartServer()
		{
			Console.WriteLine("Starting the server");
			bool start = GameServer.Instance.Start();
			return start;
		}

		public void OnAction(Hashtable parameters)
		{
			Console.WriteLine("Starting GameServer ... please wait a moment!");
			FileInfo configFile;
			FileInfo currentAssembly = null;
			if (parameters["-config"] != null)
			{
				Console.WriteLine("Using config file: " + parameters["-config"]);
				configFile = new FileInfo((String)parameters["-config"]);
			}
			else
			{
				currentAssembly = new FileInfo(Assembly.GetEntryAssembly().Location);
				configFile = new FileInfo(currentAssembly.DirectoryName + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "serverconfig.xml");
			}
			if (parameters.ContainsKey("-crashonfail")) crashOnFail = true;

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

			if (crashOnFail && GameServer.Instance.ServerStatus == EGameServerStatus.GSS_Closed)
			{
				throw new ApplicationException("Server did not start properly.");
			}

			bool run = true;

			while (run)
			{
				Console.Write("> ");
				string line = Console.ReadLine();

				if (line == null)
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
					{
						if (line.Length <= 0)
							break;

						if (line[0] == '/')
						{
							line = line.Remove(0, 1);
							line = line.Insert(0, "&");
						}

						GameClient client = new(null, null);
						client.Out = new ConsolePacketLib();

						try
						{
							bool res = ScriptMgr.HandleCommandNoPlvl(client, line);
							if (!res)
							{
								Console.WriteLine("Unknown command: " + line);
							}
						}
						catch (Exception e)
						{
							Console.WriteLine(e.ToString());
						}

						break;
					}
				}
			}

			GameServer.Instance?.Stop();
		}
	}
}
