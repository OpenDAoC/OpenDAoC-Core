using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture, Explicit]
public class MySqlDbInterfaceTests : InterfaceTests
{
	public MySqlDbInterfaceTests()
	{
		Database = MySqlDbSetUp.Database;
	}
	
	[Test]
	public override void TestEscape()
	{
		var test = "\\\"'’";
		
		Assert.AreEqual("\\\\\\\"\\'\\’", Database.Escape(test), "MySQL String Escape Test Failure...");
	}
}