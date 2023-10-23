using Core.GS;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class Train1105 : ServerTests
{
	[Test, Explicit]
	public void TrainNow()
	{
		GamePlayer player = CreateMockGamePlayer();
		Assert.IsNotNull(player);
		player.Out.SendTrainerWindow();
		return;
	}
}