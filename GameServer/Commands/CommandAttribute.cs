using System;

namespace DOL.GS
{
	/// <summary>
	/// Marks a class as a command handler
	/// </summary>
	[AttributeUsage(AttributeTargets.Class,AllowMultiple = true)]
	public class CommandAttribute : Attribute
	{
		private	string		m_cmd;
		private string[]	m_cmdAliases;
		private	string		m_header;
		private uint		m_plvl;
		private	string		m_description;
		private string[]	m_usage;

		/// <summary>
		/// Command constructor with alias
		/// </summary>
		/// <param name="cmd">Command to handle</param>
		/// <param name="alias">Other names the command goes by</param>
		/// <param name="plvl">Minimum required plvl for this command</param>
		/// <param name="desc">Description of the command</param>
		/// <param name="usage">How to use the command</param>
		public CommandAttribute(string cmd, string[] alias, ePrivLevel plvl, string desc, params string[] usage)
		{
			m_cmd=cmd;
			m_cmdAliases = alias;
			m_plvl = (uint)plvl;
			m_description = desc;
			//m_usage = new string[1];
			m_usage = usage;
		}

		/// <summary>
		/// Standard command constructor
		/// </summary>
		/// <param name="cmd">Command to handle</param>
		/// <param name="plvl">Minimum required plvl for this command</param>
		/// <param name="desc">Description of the command</param>
		/// <param name="usage">How to use the command</param>
		public CommandAttribute(string cmd, ePrivLevel plvl, string desc, params string[] usage)
		{
			m_cmd=cmd;
			m_plvl = (uint)plvl;
			m_description = desc;
			m_usage = usage;
		}

		/// <summary>
		/// Command constructor with alias and header/divider
		/// </summary>
		/// <param name="cmd">Command type to handle (e.g., "/player")</param>
		/// <param name="alias">Other names the command type goes by (e.g., '/gmhelp')</param>
		/// <param name="header">Separator for the command type (e.g., "AdminCommands.Header.Syntax.Plvl")</param>
		/// <param name="plvl">Minimum required privilege level (e.g., ePrivLevel.Admin)</param>
		/// <param name="desc">Description of the command type (e.g., "AdminCommands.Account.Description")</param>
		/// <param name="usage">Syntax/descriptions for how to structure and use all commands of this type (e.g., syntax = "AdminCommands.Account.Syntax.AccountName", desc = "AdminCommands.Account.Usage.AccountName")</param>
		public CommandAttribute(string cmd, string[] alias, string header, ePrivLevel plvl, string desc, params string[] usage)
		{
			m_cmd = cmd;
			m_cmdAliases = alias;
			m_header = header;
			m_plvl = (uint)plvl;
			m_description = desc;
			m_usage = usage;
		}

		/// <summary>
		/// Command constructor with header/divider
		/// </summary>
		/// <param name="cmd">Command type to handle (e.g., "/player")</param>
		/// <param name="header">Separator for the command type (e.g., "AdminCommands.Header.Syntax.Plvl")</param>
		/// <param name="plvl">Minimum required privilege level (e.g., ePrivLevel.Admin)</param>
		/// <param name="desc">Description of the command type (e.g., "AdminCommands.Account.Description")</param>
		/// <param name="usage">Syntax/descriptions for how to structure and use all commands of this type (e.g., syntax = "AdminCommands.Account.Syntax.AccountName", desc = "AdminCommands.Account.Usage.AccountName")</param>
		public CommandAttribute(string cmd, string header, ePrivLevel plvl, string desc, params string[] usage)
		{
			m_cmd = cmd;
			m_header = header;
			m_plvl = (uint)plvl;
			m_description = desc;
			m_usage = usage;
		}

		/// <summary>
		/// Gets the command type being handled (e.g., "/player")
		/// </summary>
		public string Cmd
		{
			get 
			{
	  			return m_cmd;
			}
		}

		/// <summary>
		/// Gets aliases for the command being handled (e.g., "/gmhelp")
		/// </summary>
		public string[] Aliases
		{
			get
			{
				return m_cmdAliases;
			}
		}

		/// <summary>
		/// Gets the header/divider for the command type (typically a translation ID, like "AdminCommands.Header.Syntax.Plvl")
		/// </summary>
		public string Header
		{
			get
			{
				return m_header;
			}
		}

		/// <summary>
		/// Gets minimum required plvl (e.g., ePrivLevel.Admin) to use the command
		/// </summary>
		public uint Level
		{
			get
			{
				return m_plvl;
			}
		}

		/// <summary>
		/// Gets the command type's general description
		/// </summary>
		public string Description
		{
			get 
			{
				return m_description;
			}
		}

		/// <summary>
		/// Gets the syntax and description for each command of that type
		/// </summary>
		public string[] Usage
		{
			get
			{
				return m_usage;
			}
		}
	}
}