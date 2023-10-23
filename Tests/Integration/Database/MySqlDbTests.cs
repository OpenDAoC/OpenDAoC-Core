using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture, Explicit]
public class MySqlDbTests : DatabaseTests
{
	public MySqlDbTests()
	{
		Database = MySqlDbSetUp.Database;
	}
}