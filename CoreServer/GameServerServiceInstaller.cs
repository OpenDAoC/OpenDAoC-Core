#if NETFRAMEWORK
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DOL.DOLServer
{
	/// <summary>
	/// Zusammenfassung f�r GameServerServiceInstaller.
	/// </summary>
	[RunInstaller(true)]
	public class GameServerServiceInstaller : Installer
	{
		private ServiceInstaller m_gameServerServiceInstaller;
		private ServiceProcessInstaller m_gameServerServiceProcessInstaller;

		public GameServerServiceInstaller()
		{
			// Instantiate installers for process and services.
			m_gameServerServiceProcessInstaller = new ServiceProcessInstaller();
			m_gameServerServiceProcessInstaller.Account = ServiceAccount.LocalSystem;

			m_gameServerServiceInstaller = new ServiceInstaller();
			m_gameServerServiceInstaller.StartType = ServiceStartMode.Manual;
			m_gameServerServiceInstaller.ServiceName = "DOL";

			Installers.Add(m_gameServerServiceProcessInstaller);
			Installers.Add(m_gameServerServiceInstaller);
		}

		public override void Install(System.Collections.IDictionary stateSaver)
		{
			Context.Parameters["assemblyPath"] += " --SERVICERUN " + Context.Parameters["commandline"];
			base.Install(stateSaver);
		}

	}
}
#endif