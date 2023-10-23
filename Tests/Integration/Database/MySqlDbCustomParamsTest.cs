using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture, Explicit]
public class MySqlDbCustomParamsTest : CustomParamsTest
{
	public MySqlDbCustomParamsTest()
	{
		Database = MySqlDbSetUp.Database;
	}
}