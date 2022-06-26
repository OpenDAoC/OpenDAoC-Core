using DOL.Database;
using System;
using System.Collections.Generic;

namespace DOL.GS {
    public static class CraftingProgressMgr {
        private static IDictionary<GamePlayer, Dictionary<eCraftingSkill, int>> _craftingChanges = new Dictionary<GamePlayer, Dictionary<eCraftingSkill, int>>();
        private static readonly object _lockObject = new();

        /// <summary>
        /// For now, this method assumes all validation has been checked by m_craftingSkills in player class
        /// This will be moved into a CraftingComponent onto the player once proof of concept it working.
        /// </summary>
        /// <param name="gamePlayer"></param>
        /// <param name="craftSkill"></param>
        /// <param name="amount"></param>
        public static void TrackChange(GamePlayer gamePlayer, Dictionary<eCraftingSkill, int> craftingChanges) {
            lock (_lockObject) {
                if (_craftingChanges.ContainsKey(gamePlayer)) {
                    _craftingChanges[gamePlayer] = craftingChanges;
                } else {
                    _craftingChanges.Add(gamePlayer, craftingChanges);
                }
            }
        }

        /// <summary>
        /// Calls save on a specific player in case they log out
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool FlushAndSaveInstance(GamePlayer player) {
            lock (_lockObject) {
                if(_craftingChanges.TryGetValue(player, out var results)) {
                    _saveInstance(player, results);
                    _craftingChanges.Remove(player);
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Saves all cached data into the DB and clears the cache
        /// </summary>
        /// <returns></returns>
        public static int Save() {
            int count = 0;
            lock (_lockObject) {
                foreach (var change in _craftingChanges) {
                    if (change.Key == null) {
                        continue;
                    }
                    _saveInstance(change.Key, change.Value);
                    count++;
                }
                _craftingChanges.Clear();
            }
            return count;
        }

        /// <summary>
        /// Single Instance
        /// </summary>
        /// <param name="change"></param>
        private static void _saveInstance(GamePlayer player, Dictionary<eCraftingSkill, int> change) {
            AccountXCrafting craftingForRealm = DOLDB<AccountXCrafting>.SelectObject(DB.Column("AccountID").IsEqualTo(player.AccountName)
                .And(DB.Column("Realm").IsEqualTo(player.Realm)));
            craftingForRealm.CraftingPrimarySkill = (byte)player.CraftingPrimarySkill;
            string cs = string.Empty;
            if (player.CraftingPrimarySkill != eCraftingSkill.NoCrafting) {
                lock (_lockObject) {
                    foreach (var de in change) {
                        if (cs.Length > 0) cs += ";";
                        cs += Convert.ToInt32(de.Key) + "|" + Convert.ToInt32(de.Value);
                    }
                }
            }
            craftingForRealm.SerializedCraftingSkills = cs;
            GameServer.Database.SaveObject(craftingForRealm);
        }
    }
}
