using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Core.Database;
using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.Server;
using log4net;

namespace Core.GS.Scripts.Custom;

/// <summary>
/// UnloadXMLCommandHandler is used to Fully unload DataBase or DataTable to a Local XML file.
/// </summary>
[Command(
	"&unloadxmldb",
	EPrivLevel.Admin,
	"Unload your Database Tables to a local XML File Repository.",
	"Usage: /unloadxmldb [FULL|TableName]")]
public class UnloadXmlCommand : ACommandHandler, ICommandHandler
{
	#region ServerProperties
	/// <summary>
	/// Set Default Path for Unloading XML Package Directory
	/// </summary>
	[ServerProperty("xmlautoload", "xml_unload_db_directory", "Enforce directory path where the XML Packages are Unloaded From Database (Relative to Scripts or Absolute...)", "dbupdater/unload")]
	public static string XML_UNLOAD_DB_DIRECTORY;
	#endregion

	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Check For UnloadXML Args
	/// </summary>
	/// <param name="client"></param>
	/// <param name="args"></param>
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}
		
		var types = LoaderUnloaderXml.GetAllDataTableTypes();
		
		var argTable = args[1];
		
		// Prepare Write Path
		var directory = Path.IsPathRooted(XML_UNLOAD_DB_DIRECTORY) ? XML_UNLOAD_DB_DIRECTORY : string.Format("{0}{1}scripts{1}{2}", GameServer.Instance.Configuration.RootDirectory, Path.DirectorySeparatorChar, XML_UNLOAD_DB_DIRECTORY);

		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);


		switch (argTable)
		{
			case "full":
				foreach (Type table in types)
				{
					var dir = directory;
					var workingType = table;
					System.Threading.Tasks.Task.Factory.StartNew(() => LoaderUnloaderXml.UnloadXMLTable(workingType, dir));
				}
			break;
			default:
				var finddir = directory;
				var findType = types.FirstOrDefault(t => t.Name.Equals(argTable, StringComparison.OrdinalIgnoreCase) || AttributeUtil.GetTableName(t).Equals(argTable, StringComparison.OrdinalIgnoreCase));
				if (findType == null)
				{
					DisplaySyntax(client);
					if (log.IsInfoEnabled)
						log.InfoFormat("Could not find table to unload with search string : {0}", argTable);
					return;
				}
				
				System.Threading.Tasks.Task.Factory.StartNew(() => LoaderUnloaderXml.UnloadXMLTable(findType, finddir));
			break;
		}
	}
}