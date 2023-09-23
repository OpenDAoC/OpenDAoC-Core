#if NETFRAMEWORK
using System;
using System.Reflection;
using System.IO;
using System.ServiceProcess;
using DOL.GS;

namespace DOL.DOLServer
{
	/// <summary>
	/// DOL System Service
	/// </summary>
	public class GameServerService : ServiceBase
	{
		public GameServerService()
		{
			this.ServiceName = "DOL";
			this.AutoLog = false;
			this.CanHandlePowerEvent = false;
			this.CanPauseAndContinue = false;
			this.CanShutdown = true;
			this.CanStop = true;
		}

		private static bool StartServer()
		{
			//TODO parse args for -config parameter!
			FileInfo dolserver = new FileInfo(Assembly.GetExecutingAssembly().Location);
			Directory.SetCurrentDirectory(dolserver.DirectoryName);
			FileInfo configFile = new FileInfo("./config/serverconfig.xml");
			GameServerConfiguration config = new GameServerConfiguration();
			if (configFile.Exists)
			{
				config.LoadFromXMLFile(configFile);
			}
			else
			{
				if (!configFile.Directory.Exists)
					configFile.Directory.Create();
				config.SaveToXMLFile(configFile);
			}

			GameServer.CreateInstance(config);

			return GameServer.Instance.Start();
		}

		private static void StopServer()
		{
			GameServer.Instance.Stop();
		}

		protected override void OnStart(string[] args)
		{
			if (!StartServer())
				throw new ApplicationException("Failed to start server!");
		}

		protected override void OnStop()
		{
			StopServer();
		}

		protected override void OnShutdown()
		{
			StopServer();
		}

		/// <summary>
		/// Gets the DOL service from the service list
		/// </summary>
		/// <returns></returns>
		public static ServiceController GetDOLService()
		{
			foreach (ServiceController svcc in ServiceController.GetServices())
			{
				if (svcc.ServiceName.ToLower().Equals("dol"))
					return svcc;
			}
			return null;
		}
	}
}
#endif