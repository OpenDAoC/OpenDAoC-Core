using System;
using System.IO;
using System.Reflection;
using Core.Database;
using Core.Database.Enums;
using NUnit.Framework;

namespace Core.Tests.Integration;

[SetUpFixture]
public class MySqlDbSetUp
{
	public MySqlDbSetUp()
	{
	}

	public static SqlObjectDatabase Database { get; set; }
	public static string ConnectionString { get; set; }

	[OneTimeSetUp]
	public void SetUp()
	{
		var CodeBase = new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).Directory;
		ConnectionString = "Server=localhost;Port=3306;Database=test_dol_database;User ID=root;Password=;Treat Tiny As Boolean=False";

		Database = (SqlObjectDatabase)ObjectDatabase.GetObjectDatabase(EConnectionType.DATABASE_MYSQL, ConnectionString);

		Console.WriteLine("DB Configured : {0}, {1}", Database.ConnectionType, ConnectionString);

		log4net.Config.BasicConfigurator.Configure(
			new log4net.Appender.ConsoleAppender
			{
				Layout = new log4net.Layout.SimpleLayout(),
				Threshold = log4net.Core.Level.Info
			});
	}

	[OneTimeTearDown]
	public void TearDown()
	{
		log4net.LogManager.Shutdown();
	}
}