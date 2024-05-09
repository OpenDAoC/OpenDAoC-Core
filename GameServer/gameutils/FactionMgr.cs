using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
    public class FactionMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Dictionary<int, Faction> Factions { get; } = [];

        private FactionMgr() { }

        public static bool Init()
        {
            IList<DbFaction> dbFactions = GameServer.Database.SelectAllObjects<DbFaction>();

            foreach(DbFaction dbFaction in dbFactions)
            {
                Faction faction = new();
                faction.LoadFromDatabase(dbFaction);
                Factions.Add(dbFaction.ID,faction);
            }

            IList<DbFactionLinks> dbFactionLinks = GameServer.Database.SelectAllObjects<DbFactionLinks>();

            foreach (DbFactionLinks dbFactionLink in dbFactionLinks)
            {
                Faction faction = GetFactionByID(dbFactionLink.LinkedFactionID);
                Faction otherFaction = GetFactionByID(dbFactionLink.FactionID);

                if (faction == null || otherFaction == null) 
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Missing faction or friend faction with id {dbFactionLink.LinkedFactionID}/{dbFactionLink.FactionID}");

                    continue;
                }

                if (dbFactionLink.IsFriend)
                    faction.FriendFactions.Add(otherFaction);
                else
                    faction.EnemyFactions.Add(otherFaction);
            }

            return true;
        }

        public static Faction GetFactionByID(int id)
        {
            return Factions.TryGetValue(id, out Faction value) ? value : null;
        }

        public static int SaveAllAggroToFaction()
        {
            if (Factions == null)
                return 0;

            int count = 0;

            foreach (Faction faction in Factions.Values)
                count += faction.SaveAggroLevels();

            return count;
        }

        public static void LoadAllAggroToFaction(GamePlayer player)
        {
            IList<DbFactionAggroLevel> factionRelations = DOLDB<DbFactionAggroLevel>.SelectObjects(DB.Column("CharacterID").IsEqualTo(player.ObjectId));

            foreach (DbFactionAggroLevel factionRelation in factionRelations)
            {
                Faction faction = GetFactionByID(factionRelation.FactionID);

                if (faction == null)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Missing faction with id {factionRelation.FactionID}");

                    continue;
                }

                faction.TryLoadAggroLevel(player, factionRelation.AggroLevel);
            }
        }

        public static bool CanLivingAttack(GameLiving attacker, GameLiving defender)
        {
            return attacker is not GameNPC npcAttacker || defender is not GameNPC defenderNpc || !npcAttacker.IsFriend(defenderNpc);
        }
    }
}
