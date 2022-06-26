using DOL.Database;
using System;
using System.Collections.Generic;

namespace DOL.GS {
    public static class CraftingProgressMgr {
        private static IDictionary<GamePlayer, Dictionary<eCraftingSkill, int>> _craftingChanges = new Dictionary<GamePlayer, Dictionary<eCraftingSkill, int>>(1000);
        private static readonly object _lockObject = new();

        /// <summary>
        /// For now, this method assumes all validation has been checked by m_craftingSkills in player class
        /// This will be moved into a CraftingComponent onto the player once proof of concept it working.
        /// </summary>
        /// <param name="gamePlayer"></param>
        /// <param name="craftSkill"></param>
        /// <param name="amount"></param>
        public static void TrackChange(GamePlayer gamePlayer, eCraftingSkill craftSkill, int amount) {
            lock (_lockObject) {                
                if (_craftingChanges.TryGetValue(gamePlayer, out var existingInstance)) {
                    if(!existingInstance.ContainsKey(craftSkill)) {
                        existingInstance.Add(craftSkill, amount);
                    } else {
                        existingInstance[craftSkill] = amount;
                    }
                } else {
                    Dictionary<eCraftingSkill, int> instanceRecord = new Dictionary<eCraftingSkill, int>();
                    instanceRecord.Add(craftSkill, amount);
                    _craftingChanges.Add(gamePlayer, instanceRecord);
                }
            }
        }

        public static int Save() {
            int count = 0;
            foreach(var change in _craftingChanges) {
                if(change.Key == null) {
                    continue;
                }
                GamePlayer player = change.Key;                
                AccountXCrafting craftingForRealm = DOLDB<AccountXCrafting>.SelectObject(DB.Column("AccountID").IsEqualTo(player.AccountName).And(DB.Column("Realm").IsEqualTo(player.Realm)));
                craftingForRealm.CraftingPrimarySkill = (byte)player.CraftingPrimarySkill;
                string cs = "";
                if (player.CraftingPrimarySkill != eCraftingSkill.NoCrafting) {
                    lock (_lockObject) {
                        foreach (KeyValuePair<eCraftingSkill, int> de in change.Value) {
                            if (cs.Length > 0) cs += ";";
                            cs += Convert.ToInt32(de.Key) + "|" + Convert.ToInt32(de.Value);
                        }
                    }
                }
                craftingForRealm.SerializedCraftingSkills = cs;
                GameServer.Database.SaveObject(craftingForRealm);
                count++;
            }
            return count;
        }
    }
}
