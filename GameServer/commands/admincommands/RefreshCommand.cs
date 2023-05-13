/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using DOL.GS;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Handles reloading individual modules' static caches.
	/// </summary>
	[Cmd("&refresh",
		// Message: '/refresh' - Refreshes specific static data caches stored via scripts and other objects.
		"AdminCommands.Refresh.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		ePrivLevel.Admin,
		// Message: Refreshes specific static data caches stored using scripts and other objects.
		"AdminCommands.Refresh.Description",
		// Message: /refresh <className>.<methodName>
		"AdminCommands.Refresh.Syntax.RefreshModule",
		// Message: Refreshes a specific module's static cache.
		"AdminCommands.Refresh.Usage.RefreshModule",
		// Message: /refresh list
		"AdminCommands.Refresh.Syntax.RefreshList",
		// Message: Returns a list of all available modules that with static caches that may be refreshed.
		"AdminCommands.Refresh.Usage.RefreshList"
	)]
	public class RefreshCommand : AbstractCommandHandler, ICommandHandler
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
					// Message: An incorrect module was specified. Use '/refresh list' to see available options.
					ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Refresh.Err.WrongModule", null);
				}
				else
				{
					// Message: [START] Refreshing the module static cache for: {0}
					ChatUtil.SendTypeMessage((int)eMsg.Important, client, "AdminCommands.Refresh.Msg.RefreshingModules", method.Key);
					
					try
					{
						object value = method.Value.Invoke(null, new object[] { });

						if (value != null)
							// Message: [RETURN] Module returned value: {0}
							ChatUtil.SendTypeMessage((int)eMsg.Important, client, "AdminCommands.Refresh.Msg.ReturnedValue", value);
					}
					catch(Exception e)
					{
						// Message: [ERROR] An unexpected issue occurred: {0}, {1}
						ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Refresh.Err.ErrorStaticCache", method.Key, e);
					}
						
					// Message: [DONE] Refreshed the static cache for: {0}
					ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Refresh.Msg.StaticCacheFinished", method.Key);
				}
			}
		}
		
		/// <summary>
		/// Short hand for displaying available module refresh commands
		/// </summary>
		/// <param name="client"></param>
		private void DisplayAvailableModules(GameClient client)
		{
			// Message: <----- '/refresh' Modules ----->
			ChatUtil.SendTypeMessage((int)eMsg.System, client, "AdminCommands.Refresh.Msg.AvailableModules", null);
			
			foreach(var mods in m_refreshCommandCache.Keys)
				ChatUtil.SendTypeMessage((int)eMsg.System, client, mods);
		}

		/// <summary>
		/// Init Refresh Command Cache looking Assembly for Refresh Command Attribute.
		/// </summary>
		[RefreshCommandAttribute]
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
