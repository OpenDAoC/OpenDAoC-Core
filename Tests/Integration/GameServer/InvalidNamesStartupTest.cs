using Core.GS;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class InvalidNamesStartupTest
{
	public InvalidNamesStartupTest()
	{
	}
	
	[Test]
	public void InvalidNamesStartup_CheckDefaultConstraintOneString_Match()
	{
		Assert.IsTrue(GameServer.Instance.PlayerManager.InvalidNames["fuck"]);
	}
	
	[Test]
	public void InvalidNamesStartup_CheckDefaultConstraintOneString_NoMatch()
	{
		Assert.IsFalse(GameServer.Instance.PlayerManager.InvalidNames["unicorn"]);
	}
	
	[Test]
	public void InvalidNamesStartup_CheckDefaultConstraintTwoString_Match()
	{
		Assert.IsTrue(GameServer.Instance.PlayerManager.InvalidNames["fu", "ck"]);
	}
	
	[Test]
	public void InvalidNamesStartup_CheckDefaultConstraintTwoString_NoMatch()
	{
		Assert.IsFalse(GameServer.Instance.PlayerManager.InvalidNames["uni", "corn"]);
	}
}