#if NETFRAMEWORK
using System.Collections;
using System.ServiceProcess;

namespace DOL.DOLServer.Actions
{
	/// <summary>
	/// Handles starting of the service, internal action, can't be used from commandline!
	/// </summary>
	public class ServiceRun : IAction
	{
		/// <summary>
		/// returns the name of this action
		/// </summary>
		public string Name
		{
			get { return "--SERVICERUN"; }
		}

		/// <summary>
		/// returns the syntax of this action
		/// </summary>
		public string Syntax
		{
			get { return null; }
		}

		/// <summary>
		/// returns the description of this action
		/// </summary>
		public string Description
		{
			get { return null; }
		}

		public void OnAction(Hashtable parameters)
		{
			ServiceBase.Run(new GameServerService());
		}
	}
}
#endif