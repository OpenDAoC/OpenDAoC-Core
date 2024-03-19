using System;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Friends;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Integration.Server
{
	/// <summary>
	/// FriendsManager Unit Tests.
	/// </summary>
	[TestFixture]
	public class FriendsManagerTest
	{		
		public FriendsManagerTest()
		{
		}
		
		[Test]
		public void FriendsManager_StartUp_NotNull()
		{
			ClassicAssert.IsNotNull(GameServer.Instance.PlayerManager.Friends);
		}
		
		[Test]
		public void FriendsManager_NoAdd_RetrieveEmptyGamePlayerList()
		{
			CollectionAssert.IsEmpty(GameServer.Instance.PlayerManager.Friends[null]);
		}
		
		[Test]
		public void FriendsManager_AddPlayer_RetrieveFriendsList()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			
			CollectionAssert.IsEmpty(gameplayer.GetFriends());
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_RemovePlayer_RetrieveEmptyFriendsList()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy" }  } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			
			CollectionAssert.IsEmpty(gameplayer.GetFriends());
		}
		
		[Test]
		public void FriendsManager_AddPlayerWithFriend_RetrieveFriendsList()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy" } } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { client.Account.Characters[0].SerializedFriendsList });
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { "buddy" });
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_AddPlayerWithFriends_RetrieveFriendsList()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), gameplayer.SerializedFriendsList);
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { "buddy", "mate", "someone" });
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_AddFriend_RetrieveFriend()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			gameplayer.AddFriend("buddy");

			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { client.Account.Characters[0].SerializedFriendsList });
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { "buddy" });
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_AddFriends_RetrieveFriend()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			gameplayer.AddFriend("buddy");
			gameplayer.AddFriend("mate");
			gameplayer.AddFriend("someone");

			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), gameplayer.SerializedFriendsList);
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { "buddy", "mate", "someone" });
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_AddDuplicate_RetrieveOnlyOne()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			gameplayer.AddFriend("buddy");
			ClassicAssert.IsFalse(gameplayer.AddFriend("buddy"));
			gameplayer.AddFriend("someone");

			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), gameplayer.SerializedFriendsList);
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { "buddy", "someone" });
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_RemoveFriend_RetrieveEmpty()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			gameplayer.AddFriend("buddy");
			CollectionAssert.IsNotEmpty(gameplayer.GetFriends());
			gameplayer.RemoveFriend("buddy");
			CollectionAssert.IsEmpty(gameplayer.GetFriends());
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_RemoveFriends_RetrieveEmpty()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			gameplayer.AddFriend("buddy");
			gameplayer.AddFriend("mate");
			gameplayer.AddFriend("someone");
			CollectionAssert.IsNotEmpty(gameplayer.GetFriends());
			gameplayer.RemoveFriend("buddy");
			gameplayer.RemoveFriend("mate");
			gameplayer.RemoveFriend("someone");
			CollectionAssert.IsEmpty(gameplayer.GetFriends());
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_RemoveNonExisting_RetrieveOne()
		{
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			gameplayer.AddFriend("buddy");
			CollectionAssert.IsNotEmpty(gameplayer.GetFriends());
			ClassicAssert.IsFalse(gameplayer.RemoveFriend("mate"));
			CollectionAssert.AreEquivalent(gameplayer.GetFriends(), new [] { "buddy" });
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_NotifyPlayerWorldEnter_ReceivePacketEmptyList()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib() { SendAddFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			client.ClientState = GameClient.eClientState.WorldEnter;
					
			CollectionAssert.IsEmpty(received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
		}
		
		[Test]
		public void FriendsManager_NotifyPlayerWorldEnterWithFriends_ReceivePacketMateList()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendAddFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate" } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);

			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			client.ClientState = GameClient.eClientState.WorldEnter;
					
			CollectionAssert.AreEquivalent(new[] { "mate" }, received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_NotifyPlayerWorldEnterWithFriendsAnon_ReceivePacketMateList()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendAddFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate" } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			var clientBuddy = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "buddy", IsAnonymous = true } } },
				Out = new TestPacketLib()
			};
			var gameplayerBuddy = new GamePlayer(clientBuddy, clientBuddy.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerBuddy);
			
			client.ClientState = GameClient.eClientState.WorldEnter;
					
			CollectionAssert.AreEquivalent(new[] { "mate" }, received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerBuddy);
		}
		
		[Test]
		public void FriendsManager_FriendsGetAnonymous_ReceiveRemovePacket()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendRemoveFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate" } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			gameplayerMate.IsAnonymous = true;
			
			CollectionAssert.AreEquivalent(new[] { "mate" }, received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_FriendsUnsetAnonymous_ReceiveAddPacket()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendAddFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate", IsAnonymous = true } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			gameplayerMate.IsAnonymous = false;
			
			CollectionAssert.AreEquivalent(new[] { "mate" }, received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_FriendsEnterGame_ReceiveAddPacket()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendAddFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate" } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			GameEventMgr.Notify(GamePlayerEvent.GameEntered, gameplayerMate);
			CollectionAssert.AreEquivalent(new[] { "mate" }, received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_FriendsEnterGameAnon_ReceiveNoAddPacket()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendAddFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate", IsAnonymous = true } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			GameEventMgr.Notify(GamePlayerEvent.GameEntered, gameplayerMate);
			ClassicAssert.IsNull(received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_FriendsExitGame_ReceiveRemovePacket()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendRemoveFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate" } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			GameEventMgr.Notify(GamePlayerEvent.Quit, gameplayerMate);
			CollectionAssert.AreEquivalent(new[] { "mate" }, received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_FriendsExitGame_ReceiveNoRemovePacket()
		{
			string[] received = null;
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { SerializedFriendsList = "buddy , mate , someone" } } },
				Out = new TestPacketLib() { SendRemoveFriendsMethod = (lib, friends) => received = friends }
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			client.Player = gameplayer;
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayer);
			var clientMate = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() { Name = "mate", IsAnonymous = true } } },
				Out = new TestPacketLib()
			};
			var gameplayerMate = new GamePlayer(clientMate, clientMate.Account.Characters[0]);
			
			GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(gameplayerMate);
			
			GameEventMgr.Notify(GamePlayerEvent.Quit, gameplayerMate);
			ClassicAssert.IsNull(received);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayer);
			GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(gameplayerMate);
		}
		
		[Test]
		public void FriendsManager_WrongMethodArguments_ThrowsException()
		{
			ClassicAssert.Throws(typeof(ArgumentNullException), () => GameServer.Instance.PlayerManager.Friends.AddPlayerFriendsListToCache(null));
			ClassicAssert.Throws(typeof(ArgumentNullException), () => GameServer.Instance.PlayerManager.Friends.RemovePlayerFriendsListFromCache(null));
			ClassicAssert.Throws(typeof(ArgumentNullException), () => GameServer.Instance.PlayerManager.Friends.AddFriendToPlayerList(null, null));
			ClassicAssert.Throws(typeof(ArgumentNullException), () => GameServer.Instance.PlayerManager.Friends.RemoveFriendFromPlayerList(null, null));
			
			var client = new GameClient(GameServer.Instance, null) {
				Account = new DbAccount() { Characters = new [] { new DbCoreCharacter() } },
				Out = new TestPacketLib()
			};
			var gameplayer = new GamePlayer(client, client.Account.Characters[0]);
			
			ClassicAssert.Throws(typeof(ArgumentNullException), () => GameServer.Instance.PlayerManager.Friends.AddFriendToPlayerList(gameplayer, null));
			ClassicAssert.Throws(typeof(ArgumentNullException), () => GameServer.Instance.PlayerManager.Friends.RemoveFriendFromPlayerList(gameplayer, null));
			ClassicAssert.Throws(typeof(ArgumentException), () => GameServer.Instance.PlayerManager.Friends.AddFriendToPlayerList(gameplayer, string.Empty));
			ClassicAssert.Throws(typeof(ArgumentException), () => GameServer.Instance.PlayerManager.Friends.RemoveFriendFromPlayerList(gameplayer, string.Empty));
			ClassicAssert.Throws(typeof(ArgumentException), () => GameServer.Instance.PlayerManager.Friends.AddFriendToPlayerList(gameplayer, " "));
			ClassicAssert.Throws(typeof(ArgumentException), () => GameServer.Instance.PlayerManager.Friends.RemoveFriendFromPlayerList(gameplayer, " "));
		}
	}
}
