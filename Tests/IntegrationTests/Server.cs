using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using NUnit.Framework;

namespace Core.Tests.Integration.Server
{
	public class ServerTests
	{
		public ServerTests()
		{
		}

		protected GamePlayer CreateMockGamePlayer()
		{
			DbCoreCharacter character= null;
			var account = GameServer.Database.SelectAllObjects<DbAccount>().FirstOrDefault();
			Assert.IsNotNull(account);

			foreach (var charact in account.Characters)
			{
				if (charact!=null)
					character = charact;
			}
			Assert.IsNotNull(character);
			
			var client = new GameClient(GameServer.Instance);
			client.Version = GameClient.eClientVersion.Version1105;
			client.Socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
			client.Account = account;
			client.PacketProcessor = new PacketProcessor(client);
			client.Out = new PacketLib1105(client);
			client.Player = new GamePlayer(client,character);
			Assert.IsNotNull(client.Player,"GamePlayer instance created");
			
			return client.Player;
		}
				
		public void cd()
		{
			Console.WriteLine("GC: "+Directory.GetCurrentDirectory());
		}
		
		#region Watch

		static long gametick;

		/// <summary>
		/// use startWatch to start taking the time
		/// </summary>
		public static void StartWatch()
		{
			//Tickcount is more accurate than gametimer ticks :)
			gametick = Environment.TickCount;
			Console.WriteLine("StartWatch: "+gametick);
		}

		/// <summary>
		/// stop watch will count the Gamticks since last call of startWatch
		/// 
		/// Note: This value does not represent the time it will take on a
		/// actual server since we have no actual user load etc...
		/// </summary>
		public static void StopWatch()
		{
			Console.WriteLine("Stop watch: "+Environment.TickCount);
			long elapsed = Environment.TickCount - gametick;
			Console.WriteLine(elapsed+" ticks(ms) elapsed");
		}
		
		#endregion
	}
}
