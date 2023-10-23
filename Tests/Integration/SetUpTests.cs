using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Core.Database;
using Core.Database.Enums;
using Core.Database.Tables;
using Core.GS;
using NUnit.Framework;

namespace Core.Tests.Integration;

/// <summary>
/// SetUpTests Start The Needed Environment for Unit Tests
/// </summary>
[SetUpFixture]
public class SetUpTests
{
	public SetUpTests()
	{
	}

	/// <summary>
	/// Create Game Server Instance for Tests
	/// </summary>
	public static void CreateGameServerInstance()
	{
		Console.WriteLine("Create Game Server Instance");
		DirectoryInfo CodeBase = new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).Directory;
		Console.WriteLine("Code Base: " + CodeBase.FullName);
		DirectoryInfo FakeRoot = CodeBase.Parent;
		Console.WriteLine("Fake Root: " + FakeRoot.FullName);

		if (GameServer.Instance == null || GameServer.Instance.GetType() != typeof(GameServer))
		{
			GameServerConfiguration config = new GameServerConfiguration();
			config.RootDirectory = FakeRoot.FullName;
			config.DBType = EConnectionType.DATABASE_SQLITE;
			config.DBConnectionString = string.Format("Data Source={0};Version=3;Pooling=False;Cache Size=1073741824;Journal Mode=Off;Synchronous=Off;Foreign Keys=True;Default Timeout=60",
											 Path.Combine(config.RootDirectory, "dol-tests-only.sqlite3.db"));
			config.Port = 0; // Auto Choosing Listen Port
			config.UDPPort = 0; // Auto Choosing Listen Port
			config.IP = System.Net.IPAddress.Parse("127.0.0.1");
			config.UDPIP = System.Net.IPAddress.Parse("127.0.0.1");
			config.RegionIP = System.Net.IPAddress.Parse("127.0.0.1");

			GameServer.LoadTestDouble(new GameServerWithDefaultDB(config));
			
			Console.WriteLine("Game Server Instance Created !");
		}
	}

	private class GameServerWithDefaultDB : GameServer
	{
		public GameServerWithDefaultDB(GameServerConfiguration config) : base(config) { }

		protected override void CheckAndInitDB()
		{
			if (m_database == null)
			{
				m_database = ObjectDatabase.GetObjectDatabase(Configuration.DBType, Configuration.DBConnectionString);

				//Load only default assembly
				var assembly = Assembly.Load("CoreDatabase");
				// Walk through each type in the assembly
				assembly.GetTypes().AsParallel().ForAll(type =>
				{
					if (!type.IsClass || type.IsAbstract)
					{
						return;
					}

					var attrib = type.GetCustomAttributes<DataTable>(false);
					if (attrib.Any())
					{
						m_database.RegisterDataObject(type);
					}
				});

				DbServerProperty loadQuestsProp = m_database.SelectObject<DbServerProperty>(DB.Column("Key").IsEqualTo("load_quests"));
				if(loadQuestsProp == null) {
					loadQuestsProp = new DbServerProperty() {
						Description = "Temporary workaround, prevents failure in ArtifactScholar region load.",
						Key = "load_quests",
						DefaultValue = "True",
						Value = "False",
						Category = "system",
					};
					m_database.SaveObject(loadQuestsProp);
				}
				if(loadQuestsProp.Value != "False") {
					loadQuestsProp.Value = "False";
					m_database.SaveObject(loadQuestsProp);
				}
			}
		}
	}

	[OneTimeSetUp]
	public virtual void Init()
	{
		CreateGameServerInstance();

		if (!GameServer.Instance.IsRunning)
		{
			Console.WriteLine("Starting GameServer");
			if (!GameServer.Instance.Start())
			{
				Console.WriteLine("Error init GameServer");
			}
		}
		else
		{
			Console.WriteLine("GameServer already running, skip init of Gameserver...");
		}
	}

	[OneTimeTearDown]
	public void Dispose()
	{
		GameServer.Instance.Stop();
	}
}