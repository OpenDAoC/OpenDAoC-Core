using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_EpicNpcArmorFactor
    {
        [Test]
        public void Calculate_ShouldReturnDefault_WhenNoPlayersOrPets()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(1.6, 24, 0, 0), Is.EqualTo(1.6).Within(1e-9));
        }

        [Test]
        public void Calculate_ShouldReduceByPlayerCount_WhenOnlyPlayersPresent()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(1.6, 24, 10, 0), Is.EqualTo(1.2).Within(1e-9));
        }

        [Test]
        public void Calculate_ShouldReduceByPetCount_WhenOnlyPetsPresent()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(1.6, 24, 0, 10), Is.EqualTo(1.5).Within(1e-9));
        }

        [Test]
        public void Calculate_ShouldClampPetCountToCap_WhenPetCountExceedsCap()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(1.6, 24, 0, 30), Is.EqualTo(1.36).Within(1e-9));
        }

        [Test]
        public void Calculate_ShouldFloorAtMinimum_WhenPlayerCountIsLarge()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(1.6, 24, 40, 0), Is.EqualTo(0.4).Within(1e-9));
        }

        [Test]
        public void Calculate_ShouldFloorAtMinimum_WhenLowerDefaultFactorAndPlayers()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(0.8, 16, 10, 0), Is.EqualTo(0.4).Within(1e-9));
        }

        [Test]
        public void Calculate_ShouldFloorAtMinimum_WhenLowerDefaultFactorAndPlayersAndPets()
        {
            Assert.That(EpicNpcArmorFactor.Calculate(0.8, 16, 20, 20), Is.EqualTo(0.4).Within(1e-9));
        }
    }
}
