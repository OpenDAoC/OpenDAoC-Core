using Core.Database;
using Core.Database.Enums;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture, Explicit]
public class MySqlDbRegisterTableTests : RegisterTableTests
{
	public MySqlDbRegisterTableTests()
	{
		Database = MySqlDbSetUp.Database;
	}
	
	protected override SqlObjectDatabase GetDatabaseV2 { get { return (SqlObjectDatabase)ObjectDatabase.GetObjectDatabase(EConnectionType.DATABASE_MYSQL, MySqlDbSetUp.ConnectionString); } }

	[Test]
	public void TestTableWithBrokenPrimaryKey()
	{
		// Destroy previous table
		Database.ExecuteNonQuery(string.Format("DROP TABLE IF EXISTS `{0}`", AttributeUtil.GetTableName(typeof(TestTableWithBrokenPrimaryV1))));
		// Create Table
		Database.RegisterDataObject(typeof(TestTableWithBrokenPrimaryV1));
		// Break Primary Key
		Database.ExecuteNonQuery(string.Format("ALTER TABLE `{0}` DROP PRIMARY KEY", AttributeUtil.GetTableName(typeof(TestTableWithBrokenPrimaryV1))));
		
		// Get a new Database Object to Trigger Migration
		var DatabaseV2 = GetDatabaseV2;
		
		// Trigger False Migration
		DatabaseV2.RegisterDataObject(typeof(TestTableWithBrokenPrimaryV2));
		
		var adds = DatabaseV2.AddObject(new [] {
			                                new TestTableWithBrokenPrimaryV2 { PrimaryKey = 1 },
			                                new TestTableWithBrokenPrimaryV2 { PrimaryKey = 1 },
		                                });
		
		Assert.IsFalse(adds, "Primary Key was not restored and duplicate key were inserted !");
	}
}