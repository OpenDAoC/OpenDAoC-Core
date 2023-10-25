using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture, Explicit]
public class MySqlDatabaseTypeTests : DatabaseTypeTests
{
	public MySqlDatabaseTypeTests()
	{
		Database = MySqlDbSetUp.Database;
	}
}