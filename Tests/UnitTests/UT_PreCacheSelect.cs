using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Database.Handlers;
using NUnit.Framework;

namespace DOL.Tests.UnitTests;

[TestFixture]
public class UT_PreCacheSelect
{
    private string _databaseFile;

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_databaseFile))
            File.Delete(_databaseFile);
    }

    [Test]
    public void SelectObjects_QueriesPreCacheWithoutDatabaseAccess()
    {
        CountingSqliteObjectDatabase database = CreateDatabase();
        Seed(database);
        database.ResetConnectionCount();

        List<PreCachedSelectObject> selected = database.SelectObjects<PreCachedSelectObject>(
            DB.Column(nameof(PreCachedSelectObject.GroupId)).IsEqualTo("GROUP-1")
                .And(DB.Column(nameof(PreCachedSelectObject.Value)).IsLike("value-%"))
                .OrderBy(DB.Column(nameof(PreCachedSelectObject.Id)), descending: true, limit: 1));
        List<PreCachedSelectObject> nonNullValues = database.SelectObjects<PreCachedSelectObject>(
            DB.Column(nameof(PreCachedSelectObject.Value)).IsNotNull());

        Assert.Multiple(() =>
        {
            Assert.That(selected.Select(item => item.Id), Is.EqualTo(new[] { "item-b" }));
            Assert.That(nonNullValues.Select(item => item.Id), Is.EquivalentTo(new[] { "item-a", "item-b" }));
            Assert.That(database.ConnectionCount, Is.Zero);
        });
    }

    [Test]
    public void MultipleSelectObjects_QueriesPreCacheInBatchOrder()
    {
        CountingSqliteObjectDatabase database = CreateDatabase();
        Seed(database);
        database.ResetConnectionCount();

        List<List<PreCachedSelectObject>> selected = database.MultipleSelectObjects<PreCachedSelectObject>(
        [
            DB.Column(nameof(PreCachedSelectObject.Id)).IsIn(new[] { "item-b" }),
            DB.Column(nameof(PreCachedSelectObject.GroupId)).IsEqualTo("group-1")
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(selected[0].Select(item => item.Id), Is.EqualTo(new[] { "item-b" }));
            Assert.That(selected[1].Select(item => item.Id), Is.EquivalentTo(new[] { "item-a", "item-b" }));
            Assert.That(database.ConnectionCount, Is.Zero);
        });
    }

    private CountingSqliteObjectDatabase CreateDatabase()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"dol-precache-select-{Guid.NewGuid():N}.sqlite");
        CountingSqliteObjectDatabase database = new($"Data Source={_databaseFile};Version=3;Pooling=False");
        database.RegisterDataObject(typeof(PreCachedSelectObject));
        return database;
    }

    private static void Seed(IObjectDatabase database)
    {
        Assert.That(database.AddObject(new DataObject[]
        {
            new PreCachedSelectObject { Id = "item-a", GroupId = "group-1", Value = "value-a" },
            new PreCachedSelectObject { Id = "item-b", GroupId = "group-1", Value = "value-b" }
        }), Is.True);
    }

    private sealed class CountingSqliteObjectDatabase : SqliteObjectDatabase
    {
        public int ConnectionCount { get; private set; }

        public CountingSqliteObjectDatabase(string connectionString) : base(connectionString) { }

        public void ResetConnectionCount() => ConnectionCount = 0;

        protected override void OpenConnection(DbConnection connection)
        {
            ConnectionCount++;
            base.OpenConnection(connection);
        }
    }
}

[DataTable(TableName = "PreCachedSelectObject", PreCache = true)]
public class PreCachedSelectObject : DataObject
{
    [PrimaryKey]
    public string Id { get; set; }

    [DataElement(AllowDbNull = false, Index = true)]
    public string GroupId { get; set; }

    [DataElement(AllowDbNull = false)]
    public string Value { get; set; }
}
