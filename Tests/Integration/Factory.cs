using System;
using System.IO;
using System.Reflection;
using Core.Database;
using Core.Database.Enums;

namespace Core.Tests.Integration;

public partial class Create
{
    private static string SQLiteDBPath
           => Path.Combine(new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).Directory.FullName, "temporary.sqlite3.db");

    public static IObjectDatabase TemporarySQLiteDB()
    {
        if (File.Exists(SQLiteDBPath)) File.Delete(SQLiteDBPath);

        IObjectDatabase sqliteDB;

        var configDBConnectionString = $"Data Source={SQLiteDBPath};Version=3;Pooling=False;Cache Size=1073741824;Journal Mode=Off;Synchronous=Off;Foreign Keys=True;Default Timeout=60";
        sqliteDB = ObjectDatabase.GetObjectDatabase(EConnectionType.DATABASE_SQLITE, configDBConnectionString);
        return sqliteDB;
    }
}