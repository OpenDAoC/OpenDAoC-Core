using System;
using Core.Base.Enums;
using Core.GS;
using NUnit.Framework;

namespace Core.Tests.Integration.Server
{
	[TestFixture]
	public class ServerTest
	{
		public ServerTest()
		{
		}
		
		[Test]
		public void TestGameServerStartup()
		{
			Console.WriteLine("Test GameServer Startup...");
			Assert.NotNull(GameServer.Instance);
			Assert.IsTrue(GameServer.Instance.IsRunning);
			Assert.AreEqual(GameServer.Instance.ServerStatus, EGameServerStatus.GSS_Open);
		}
	}
}
