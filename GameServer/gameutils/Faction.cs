using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private List<GamePlayer> _characterIdsToSave;

        public string Name { get; private set; }
        public int Id { get; private set; }
        public HashSet<Faction> FriendFactions { get; private set; }
        public HashSet<Faction> EnemyFactions { get; private set; }
        public ConcurrentDictionary<string, int> AggroToPlayers { get; private set; }

        public Faction()
        {
            Name = string.Empty;
            FriendFactions = new HashSet<Faction>();
            EnemyFactions = new HashSet<Faction>();
            AggroToPlayers = new ConcurrentDictionary<string, int>();
            _characterIdsToSave = new List<GamePlayer>();
        }

        public void AddFriendFaction(Faction faction)
        {
            FriendFactions.Add(faction);
        }

        public void RemoveFriendFaction(Faction faction)
        {
            FriendFactions.Remove(faction);
        }

        public void AddEnemyFaction(Faction faction)
        {
            EnemyFactions.Add(faction);
        }

        public void RemoveEnemyFaction(Faction faction)
        {
            EnemyFactions.Remove(faction);
        }

        public void LoadFromDatabase(DBFaction dbFaction)
        {
            Name = dbFaction.Name;
            Id = dbFaction.ID;
            _baseAggroLevel = dbFaction.BaseAggroLevel;
        }

        public void SaveFactionAggroToPlayers()
        {
            lock (((ICollection) _characterIdsToSave).SyncRoot)
            {
                foreach (GamePlayer player in _characterIdsToSave)
                    SaveFactionAggroToPlayer(player);

                _characterIdsToSave.Clear();
            }
        }

        private void SaveFactionAggroToPlayer(GamePlayer player)
        {
            DBFactionAggroLevel dbFactionAggroLevel = DOLDB<DBFactionAggroLevel>.SelectObject(DB.Column("CharacterID").IsEqualTo(player.ObjectId).And(DB.Column("FactionID").IsEqualTo(Id)));

            if (dbFactionAggroLevel == null)
            {
                dbFactionAggroLevel = new DBFactionAggroLevel
                {
                    AggroLevel = AggroToPlayers[player.ObjectId],
                    CharacterID = player.ObjectId,
                    FactionID = Id
                };

                GameServer.Database.AddObject(dbFactionAggroLevel);
            }
            else
            {
                dbFactionAggroLevel.AggroLevel = AggroToPlayers[player.ObjectId];
                GameServer.Database.SaveObject(dbFactionAggroLevel);
            }
        }

        public void KillMember(GamePlayer killer)
        {
            foreach (Faction faction in FriendFactions)
                faction.ChangeAggroLevel(killer, INCREASE_AGGRO_AMOUNT);

            foreach (Faction faction in EnemyFactions)
                faction.ChangeAggroLevel(killer, DECREASE_AGGRO_AMOUNT);
        }

        private void ChangeAggroLevel(GamePlayer player, int amount)
        {
            if (!_characterIdsToSave.Contains(player))
                _characterIdsToSave.Add(player);

            int oldAggro = AggroToPlayers.GetOrAdd(player.ObjectId, (key) => _baseAggroLevel);
            int newAggro = oldAggro + amount;

            if (newAggro < MIN_AGGRO_VALUE)
                newAggro = MIN_AGGRO_VALUE;
            else if (newAggro > MAX_AGGRO_VALUE)
                newAggro = MAX_AGGRO_VALUE;

            if (newAggro != oldAggro && Util.Chance(20))
                AggroToPlayers[player.ObjectId] = newAggro;

            string message = $"Your relationship with {Name} has {(amount > 0 ? "decreased" : "increased")}";
            player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public int GetAggroToFaction(GamePlayer player)
        {
            return AggroToPlayers.TryGetValue(player.ObjectId, out int value) ? value : _baseAggroLevel;
        }
    }
}
