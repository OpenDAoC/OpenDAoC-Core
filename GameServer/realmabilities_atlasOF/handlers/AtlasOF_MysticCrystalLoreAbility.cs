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
		public AtlasOF_MysticCrystalLoreAbility(DBAbility dba, int level) : base(dba, level) { }

        public override int CostForUpgrade(int level)
        {
			switch (level)
            {
                case 0: return 3;
                case 1: return 6;
                case 2: return 10;
                default: return 1000;
            }
        }
        
        public override int GetReUseDelay(int level)
        {
	        return 300;
        }
    }
}