#if NETFRAMEWORK
using System;
using System.Collections;
using System.ServiceProcess;

namespace DOL.DOLServer.Actions
{
	/// <summary>
	/// Handles service stop requests of the gameserver
	/// </summary>
	public class ServiceStop : IAction
	{
		/// <summary>
		/// returns the name of this action
		/// </summary>
		public string Name
		{
			get { return "--servicestop"; }
		}

		/// <summary>
		/// returns the syntax of this action
		/// </summary>
		public string Syntax
		{
			get { return "--servicestop"; }
		}

		/// <summary>
		/// returns the description of this action
		/// </summary>
		public string Description
		{
			get { return "Stops the DOL system service"; }
		}

		public void OnAction(Hashtable parameters)
		{
			ServiceController svcc = GameServerService.GetDOLService();
			if (svcc == null)
			{
				Console.WriteLine("You have to install the service first!");
				return;
			}
			if (svcc.Status == ServiceControllerStatus.StartPending)
			{
				Console.WriteLine("Server is still starting, please check the logfile for progress information!");
				return;
			}
			if (svcc.Status != ServiceControllerStatus.Running)
			{
				Console.WriteLine("The DOL service is not running");
				return;
			}
			try
			{
				Console.WriteLine("Stopping the DOL service...");
				svcc.Stop();
				svcc.WaitForStatus(ServiceControllerStatus.Stopped);
				Console.WriteLine("Finished!");
			}
			catch (InvalidOperationException e)
			{
				Console.WriteLine("Could not stop the DOL service!");
				Console.WriteLine(e.Message);
				return;
			}
		}
	}
}
#endif