using Core.GS.AI;
using NUnit.Framework;

namespace Core.Tests.Units;

[TestFixture]
class UnitControlledNpcBrain
{
    [Test]
    public void GetPlayerOwner_InitWithPlayer_Player()
    {
        var player = new UnitFakePlayer();
        var brain = new ControlledNpcBrain(player);

        var actual = brain.GetPlayerOwner();

        var expected = player;
        Assert.AreEqual(expected, actual);
    }
}