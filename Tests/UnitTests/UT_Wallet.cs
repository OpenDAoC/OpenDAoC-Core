using System;
using DOL.GS;
using NUnit.Framework;

namespace DOL.Tests.Unit.Gameserver
{
    public class UT_Wallet
    {
        private FakePlayer _player;
        private Wallet _wallet;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            FakeServer.Load();
            _player = new();
        }

        [SetUp]
        public void Setup()
        {
            _wallet = new(_player);
        }

        [Test]
        public void GetMoney_InitiallyZero()
        {
            Assert.That(_wallet.GetMoney(), Is.EqualTo(0));
        }

        [Test]
        public void LoadMoney_SetsAmount_WhenZero()
        {
            _wallet.LoadMoney(1000);
            Assert.That(_wallet.GetMoney(), Is.EqualTo(1000));
        }

        [Test]
        public void LoadMoney_Throws_IfAlreadyLoaded()
        {
            _wallet.LoadMoney(1000);
            Assert.Throws<InvalidOperationException>(() => _wallet.LoadMoney(500));
        }

        [Test]
        public void LoadMoney_Throws_IfNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.LoadMoney(-1));
        }

        [Test]
        public void AddMoney_IncreasesAmount()
        {
            _wallet.LoadMoney(500);
            _wallet.AddMoney(200);
            Assert.That(_wallet.GetMoney(), Is.EqualTo(700));
        }

        [Test]
        public void RemoveMoney_DecreasesAmount()
        {
            _wallet.LoadMoney(1000);
            bool result = _wallet.RemoveMoney(400);
            Assert.That(result, Is.True);
            Assert.That(_wallet.GetMoney(), Is.EqualTo(600));
        }

        [Test]
        public void RemoveMoney_ReturnsFalse_IfInsufficientFunds()
        {
            bool result = _wallet.RemoveMoney(100);
            Assert.That(result, Is.False);
            Assert.That(_wallet.GetMoney(), Is.EqualTo(0));
        }

        [Test]
        public void RemoveMoney_ReturnsFalse_IfInsufficientFunds_AndDoesNotRemoveMoney()
        {
            _wallet.LoadMoney(300);
            bool result = _wallet.RemoveMoney(500);
            Assert.That(result, Is.False);
            Assert.That(_wallet.GetMoney(), Is.EqualTo(300));
        }
    }
}
