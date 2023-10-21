using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.GS.Commands
{
	/// <summary>
	/// Refresh Command Handler to handle resetting Object using Static Cache.
	/// </summary>
	[Command("&refresh",
		EPrivLevel.Admin,
		"Refresh some specific static data cache stored in scripts or other objects",
		"/refresh list | ClassName 'dot' MethodName"
		)]
	public class RefreshCommand : ACommandHandler, ICommandHandler
	{
		private static readonly Dictionary<string, MethodInfo> m_refreshCommandCache = new Dictionary<string, MethodInfo>();
		private static volatile bool m_initialized = false;
		
		/// <summary>
		/// Command Handling Refreshs.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="args"></param>
		public void OnCommand(GameClient client, string[] args)
		{
			// Init Refresh Attribute Lookup
			if (!m_initialized)
			{
				m_initialized = true;
				InitRefreshCmdCache();
			}
			
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				DisplayAvailableModules(client);
			}
			
			// Join args
			string arg = string.Join(" ", args.Skip(1)).Trim();
			if (string.IsNullOrEmpty(arg))
			{
				DisplaySyntax(client);
				DisplayAvailableModules(client);
				return;
			}
			
			// Check if arg is "list" or try our module directory
			if(arg.ToLower().Equals("list"))
			{
				DisplayAvailableModules(client);
				return;
			}
			else
			{
				var method = m_refreshCommandCache.FirstOrDefault(k => k.Key.ToLower().Equals(arg.ToLower()));
				
				if (method.Value == null)
				{
					DisplayMessage(client, "Wrong Module argument given... try /refresh list!");
				}
				else
				{
					DisplayMessage(client, string.Format("--- Refreshing Module's static cache for: {0}", method.Key));
					try
					{
						object value = method.Value.Invoke(null, new object[] { });
						if (value != null)
							DisplayMessage(client, string.Format("--- Module returned value: {0}", value));
					}
					catch(Exception e)
					{
						DisplayMessage(client, string.Format("-!- Error while refreshing Module's static cache for: {0} - {1}", method.Key, e));
					}
						
					DisplayMessage(client, string.Format("-.- Refresh Module's static cache Finished for: {0}", method.Key));
				}
			}
		}
		
		/// <summary>
		/// Short hand for displaying available module refresh commands
		/// </summary>
		/// <param name="client"></param>
		private void DisplayAvailableModules(GameClient client)
		{
			DisplayMessage(client, "Available Refreshables Modules: ");
			foreach(var mods in m_refreshCommandCache.Keys)
				DisplayMessage(client, mods);
		}

		/// <summary>
		/// Init Refresh Command Cache looking Assembly for Refresh Command Attribute.
		/// </summary>
		[RefreshCommand]
		public static void InitRefreshCmdCache()
		{
			m_refreshCommandCache.Clear();
		
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in asm.GetTypes())
				{
					foreach (MethodInfo method in type.GetMethods())
					{
						// Properties are Static
						if (!method.IsStatic)
							continue;
						
						// Properties shoud contain a property attribute
						object[] attribs = method.GetCustomAttributes(typeof(RefreshCommandAttribute), false);
						if (attribs.Length == 0)
							continue;
						
						m_refreshCommandCache[string.Format("{0}.{1}", type.Name, method.Name)] = method;
					}
				}
			}
		}
	}
}
