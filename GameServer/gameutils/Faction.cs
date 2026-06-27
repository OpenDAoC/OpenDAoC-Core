using System;
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
        private readonly Lock _saveLoadLock = new();

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
                    string characterId = pair.Key;
                    bool isDisconnected = playerAggro.Player.Client.ClientState is GameClient.eClientState.Disconnected;

                    // ICollection.Remove ensures the item is only removed if both the key and the exact struct value still match.
                    // If they reconnected on another thread, TryLoadAggroLevel created a new struct, and this safely does nothing.
                    if (isDisconnected)
                        ((ICollection<KeyValuePair<string, AggroLevel>>) _aggroLevels).Remove(pair);

                    if (!playerAggro.Dirty)
                        continue;

                    int aggro = playerAggro.Aggro;
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

                    if (!isDisconnected)
                        _aggroLevels.TryUpdate(characterId, playerAggro with { Dirty = false }, playerAggro);

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
                _aggroLevels.AddOrUpdate(
                    player.ObjectId,
                    static (_, arg) => new(arg.Player, arg.Aggro),
                    static (_, oldValue, arg) => oldValue with { Player = arg.Player },
                    (Player: player, Aggro: aggro)
                );
            }
        }

        private void ChangeAggroLevel(GamePlayer player, int amount)
        {
            if (Util.Chance(20))
            {
                _aggroLevels.AddOrUpdate(
                    player.ObjectId,
                    static (_, arg) =>
                    {
                        int newAggro = Math.Clamp(arg.BaseAggro + arg.Amount, MIN_AGGRO_VALUE, MAX_AGGRO_VALUE);
                        return new(arg.Player, newAggro, Dirty: newAggro != arg.BaseAggro);
                    },
                    static (_, oldValue, arg) =>
                    {
                        int newAggro = Math.Clamp(oldValue.Aggro + arg.Amount, MIN_AGGRO_VALUE, MAX_AGGRO_VALUE);
                        return newAggro == oldValue.Aggro ? oldValue : oldValue with { Aggro = newAggro, Dirty = true };
                    },
                    (Player: player, BaseAggro: _baseAggroLevel, Amount: amount)
                );
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
            FRIENDLY,
            NEUTRAL,
            HOSTILE,
            AGGRESIVE
        }

        public readonly record struct AggroLevel(GamePlayer Player, int Aggro, bool Dirty = false);
    }
}
