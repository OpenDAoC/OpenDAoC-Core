using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Network;

namespace DOL.GS.Tests.Integration
{
    public static class TestWorld
    {
        public const int ZONE_SIZE = 65536;

        private static readonly SessionIdAllocator _sessions = new();
        private static readonly PropertyInfo _gameLoopTime = typeof(GameLoop).GetProperty(nameof(GameLoop.GameLoopTime), BindingFlags.Public | BindingFlags.Static);
        private static int _playerCounter;

        public static long GameLoopTime => GameLoop.GameLoopTime;

        public static Region CreateRegion(ushort id)
        {
            Region region = WorldMgr.GetRegion(id);

            if (region != null)
                return region;

            region = WorldMgr.RegisterRegion(new RegionData { Id = id, Name = $"test-{id}", Description = $"test-{id}" });
            region.Zones.Add(new Zone(region, id, $"test-zone-{id}", 0, 0, ZONE_SIZE, ZONE_SIZE, 0, false, 0, false, 0, 0, 0, 0, 0));
            return region;
        }

        public static GamePlayer CreatePlayer(Region region, int x, int y, int z = 0, uint privLevel = (uint) ePrivLevel.Player, eRealm realm = eRealm.Albion, int characterClass = 1)
        {
            int index = Interlocked.Increment(ref _playerCounter);
            GameClient client = new(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            client.Out = new RecordingPacketLib(client);
            client.PacketProcessor = new PacketProcessor(client);
            client.Account = new DbAccount { Name = $"testacct{index}", PrivLevel = privLevel, Realm = (int) realm };
            client.OnConnect(new SessionId(_sessions));

            DbCoreCharacter character = new()
            {
                Name = $"testplayer{index}",
                AccountName = client.Account.Name,
                Realm = (int) realm,
                Class = characterClass,
                Level = 50,
                Region = region.ID,
                Xpos = x,
                Ypos = y,
                Zpos = z
            };

            GameServer.Database.AddObject(character);
            GamePlayer player = new(client, character);
            player.InternalID = character.ObjectId;
            player.SetCharacterClass(characterClass);
            client.Player = player;
            client.ClientState = GameClient.eClientState.Playing;
            player.CurrentRegion = region;
            player.X = x;
            player.Y = y;
            player.Z = z;
            player.AddToWorld();
            player.Health = player.MaxHealth;
            player.Mana = player.MaxMana;
            player.Endurance = player.MaxEndurance;
            Tick(0);
            return player;
        }

        public static void RemovePlayer(GamePlayer player)
        {
            player.Client.ClientState = GameClient.eClientState.Disconnected;
            player.RemoveFromWorld();
            Tick(0);
        }

        public static GameNPC AddNpc(GameNPC npc, Region region, int x, int y, int z = 0)
        {
            npc.CurrentRegion = region;
            npc.X = x;
            npc.Y = y;
            npc.Z = z;
            npc.AddToWorld();
            Tick(0);
            (npc.Brain as StandardMobBrain)?.Think();
            return npc;
        }

        public static void ClearActiveEncounters()
        {
            foreach (RaidEncounter encounter in RaidEncounter.GetActiveEncounters())
                encounter.Clear();
        }

        public static void SetGameLoopTime(long time)
        {
            _gameLoopTime.SetValue(null, time);
        }

        public static void Tick(int elapsedMs = 50)
        {
            SetGameLoopTime(GameLoop.GameLoopTime + elapsedMs);
            TimerService.Instance.Tick();
            ClientService.Instance.BeginTick();
            AttackService.Instance.Tick();
            CastingService.Instance.Tick();
            EffectService.Instance.Tick();
            EffectListService.Instance.BeginTick();
            EffectListService.Instance.EndTick();
            MovementService.Instance.Tick();
            ReaperService.Instance.Tick();
            ZoneService.Instance.Tick();
            ClientService.Instance.EndTick();
        }

        public static void Advance(int totalMs, int stepMs = 50)
        {
            for (int elapsed = 0; elapsed < totalMs; elapsed += stepMs)
                Tick(Math.Min(stepMs, totalMs - elapsed));
        }

        public static void SetGameHour(double hour)
        {
            const double NIGHT_FACTOR = 1.25;
            double linearHours = hour <= 6
                ? hour / NIGHT_FACTOR
                : hour <= 18
                    ? 6 / NIGHT_FACTOR + (hour - 6)
                    : 6 / NIGHT_FACTOR + 12 + (hour - 18) / NIGHT_FACTOR;

            WorldMgr.ChangeGameTime(Properties.WORLD_DAY_INCREMENT, linearHours / 24.0);
            Tick(0);
        }

        public static List<string> Messages(GamePlayer player)
        {
            return ((RecordingPacketLib) player.Out).Messages;
        }

        public sealed class RecordingPacketLib : PacketLib1129
        {
            public List<string> Messages { get; } = new();

            public RecordingPacketLib(GameClient client) : base(client) { }

            public override void SendMessage(string msg, eChatType type, eChatLoc loc)
            {
                lock (Messages)
                    Messages.Add(msg);
            }
        }
    }
}
