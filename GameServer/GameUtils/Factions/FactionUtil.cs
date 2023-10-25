using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;

namespace Core.GS.GameUtils;

public class FactionUtil
{
    private const int DECREASE_AGGRO_AMOUNT = -1;
    private const int INCREASE_AGGRO_AMOUNT = 1;
    private const int MAX_AGGRO_VALUE = 100;
    private const int MIN_AGGRO_VALUE = -100;

    public int _baseAggroLevel;
    private List<GamePlayer> _characterIdsToSave;

    public string Name { get; private set; }
    public int Id { get; private set; }
    public HashSet<FactionUtil> FriendFactions { get; private set; }
    public HashSet<FactionUtil> EnemyFactions { get; private set; }
    public ConcurrentDictionary<string, int> AggroToPlayers { get; private set; }

    public FactionUtil()
    {
        Name = string.Empty;
        FriendFactions = new HashSet<FactionUtil>();
        EnemyFactions = new HashSet<FactionUtil>();
        AggroToPlayers = new ConcurrentDictionary<string, int>();
        _characterIdsToSave = new List<GamePlayer>();
    }

    public void AddFriendFaction(FactionUtil faction)
    {
        FriendFactions.Add(faction);
    }

    public void RemoveFriendFaction(FactionUtil faction)
    {
        FriendFactions.Remove(faction);
    }

    public void AddEnemyFaction(FactionUtil faction)
    {
        EnemyFactions.Add(faction);
    }

    public void RemoveEnemyFaction(FactionUtil faction)
    {
        EnemyFactions.Remove(faction);
    }

    public void LoadFromDatabase(DbFaction dbFaction)
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
        DbFactionAggroLevel dbFactionAggroLevel = CoreDb<DbFactionAggroLevel>.SelectObject(DB.Column("CharacterID").IsEqualTo(player.ObjectId).And(DB.Column("FactionID").IsEqualTo(Id)));

        if (dbFactionAggroLevel == null)
        {
            dbFactionAggroLevel = new DbFactionAggroLevel
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
        foreach (FactionUtil faction in FriendFactions)
            faction.ChangeAggroLevel(killer, INCREASE_AGGRO_AMOUNT);

        foreach (FactionUtil faction in EnemyFactions)
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
        player.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
    }

    public int GetAggroToFaction(GamePlayer player)
    {
        return AggroToPlayers.TryGetValue(player.ObjectId, out int value) ? value : _baseAggroLevel;
    }
}