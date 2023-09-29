#if NETFRAMEWORK
using System;
using System.Collections;
using System.Configuration.Install;
using System.Reflection;

namespace DOL.DOLServer.Actions
{
	/// <summary>
	/// Handles service uninstall requests of the gameserver
	/// </summary>
	public class ServiceUninstall : IAction
	{
		/// <summary>
		/// returns the name of this action
		/// </summary>
		public string Name
		{
			get { return "--serviceuninstall"; }
		}

		/// <summary>
		/// returns the syntax of this action
		/// </summary>
		public string Syntax
		{
			get { return "--serviceuninstall"; }
		}

		/// <summary>
		/// returns the description of this action
		/// </summary>
		public string Description
		{
			get { return "Uninstalls the DOL system service"; }
		}

		public void OnAction(Hashtable parameters)
		{
			System.Configuration.Install.AssemblyInstaller asmInstaller = new AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[1] { "/LogToConsole=false" });
			Hashtable rollback = new Hashtable();
			if (GameServerService.GetDOLService() == null)
			{
				Console.WriteLine("No service named \"DOL\" found!");
				return;
			}
			Console.WriteLine("Uninstalling DOL system service...");
			try
			{
				asmInstaller.Uninstall(rollback);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error uninstalling system service");
				Console.WriteLine(e.Message);
				return;
			}
			Console.WriteLine("Finished!");
		}
	}
}
#endif