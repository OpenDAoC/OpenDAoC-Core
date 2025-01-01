using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Faction
    {
        private const int DECREASE_AGGRO_AMOUNT = -1;
        private const int INCREASE_AGGRO_AMOUNT = 1;
        private const int MAX_AGGRO_VALUE = 100;
        private const int MIN_AGGRO_VALUE = -100;

        public int _baseAggroLevel;
        private ConcurrentDictionary<string, AggroLevel> _aggroLevels = [];
        private readonly Lock _saveLoadLock = new(); // Used to prevent `SaveAggroLevels` from removing a player from `_aggroLevels` while `TryLoadAggroLevel` is updating the `GamePlayer` reference.

        public string Name { get; private set; } = string.Empty;
        public int Id { get; private set; }
        public HashSet<Faction> FriendFactions { get; } = [];
        public HashSet<Faction> EnemyFactions { get; } = [];

        public Faction()
        {
            FriendFactions.Add(this);
        }

        public void LoadFromDatabase(DbFaction dbFaction)
        {
            Name = dbFaction.Name;
            Id = dbFaction.ID;
            _baseAggroLevel = dbFaction.BaseAggroLevel;
        }

        public int SaveAggroLevels()
        {
            int count = 0;

            lock (_saveLoadLock)
            {
                foreach (KeyValuePair<string, AggroLevel> pair in _aggroLevels)
                {
                    AggroLevel playerAggro = pair.Value;

                    if (!playerAggro.Dirty)
                        continue;

                    playerAggro.Dirty = false;
                    int aggro = playerAggro.Aggro;
                    string characterId = pair.Key;
                    DbFactionAggroLevel dbFactionAggroLevel = DOLDB<DbFactionAggroLevel>.SelectObject(DB.Column("CharacterID").IsEqualTo(characterId).And(DB.Column("FactionID").IsEqualTo(Id)));

                    if (dbFactionAggroLevel == null)
                    {
                        dbFactionAggroLevel = new DbFactionAggroLevel
                        {
                            AggroLevel = aggro,
                            CharacterID = characterId,
                            FactionID = Id
                        };

                        GameServer.Database.AddObject(dbFactionAggroLevel);
                    }
                    else
                    {
                        dbFactionAggroLevel.AggroLevel = aggro;
                        GameServer.Database.SaveObject(dbFactionAggroLevel);
                    }

                    if (playerAggro.Player.Client.ClientState == GameClient.eClientState.Disconnected)
                        _aggroLevels.TryRemove(pair);

                    count++;
                }
            }

            return count;
        }

        public void OnMemberKilled(GamePlayer killer)
        {
            foreach (Faction faction in FriendFactions)
                faction.ChangeAggroLevel(killer, INCREASE_AGGRO_AMOUNT);

            foreach (Faction faction in EnemyFactions)
                faction.ChangeAggroLevel(killer, DECREASE_AGGRO_AMOUNT);
        }

        public void TryLoadAggroLevel(GamePlayer player, int aggro)
        {
            lock (_saveLoadLock)
            {
                // Update our `GamePlayer` reference if it's new.
                _aggroLevels.AddOrUpdate(player.ObjectId, Add, Update);
            }

            AggroLevel Add(string characterId)
            {
                return new(player, aggro);
            }

            AggroLevel Update(string characterId, AggroLevel oldValue)
            {
                oldValue.Player = player;
                return oldValue;
            }
        }

        private void ChangeAggroLevel(GamePlayer player, int amount)
        {
            if (Util.Chance(20))
            {
                AggroLevel playerAggro = _aggroLevels.GetOrAdd(player.ObjectId, (key) => new(player, _baseAggroLevel));
                int oldAggro = playerAggro.Aggro;
                int newAggro = oldAggro + amount;

                if (newAggro < MIN_AGGRO_VALUE)
                    newAggro = MIN_AGGRO_VALUE;
                else if (newAggro > MAX_AGGRO_VALUE)
                    newAggro = MAX_AGGRO_VALUE;

                if (newAggro != oldAggro)
                {
                    playerAggro.Aggro = newAggro;
                    playerAggro.Dirty = true;
                }
            }

            string message = $"Your relationship with {Name} has {(amount > 0 ? "decreased" : "increased")}";
            player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public Standing GetStandingToFaction(GamePlayer player)
        {
            int aggro = _aggroLevels.TryGetValue(player.ObjectId, out AggroLevel playerAggro) ? playerAggro.Aggro : _baseAggroLevel;

            if (aggro > 75)
                return Standing.AGGRESIVE;
            else if (aggro > 50)
                return Standing.HOSTILE;
            else if (aggro > 25)
                return Standing.NEUTRAL;

            return Standing.FRIENDLY;
        }

        public enum Standing
        {
            // From least aggressive to most aggressive.
            FRIENDLY,
            NEUTRAL,
            HOSTILE,
            AGGRESIVE
        }

        public class AggroLevel(GamePlayer player, int aggro)
        {
            public GamePlayer Player { get; set; } = player;
            public int Aggro { get; set; } = aggro;
            public bool Dirty { get; set; }
        }
    }
}
