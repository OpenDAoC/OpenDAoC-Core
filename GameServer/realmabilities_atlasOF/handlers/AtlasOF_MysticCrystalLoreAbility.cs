using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Mystic Crystal Lore, power heal
	/// </summary>
	public class AtlasOF_MysticCrystalLoreAbility : MysticCrystalLoreAbility
	{
		public AtlasOF_MysticCrystalLoreAbility(DbAbility dba, int level) : base(dba, level) { }

        public override int CostForUpgrade(int currentLevel) { return AtlasRAHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }
        
        public override int GetReUseDelay(int level)
        {
	        return 300;
        }
    }
}