using System;
using System.Collections;
using DOL.Database;
using DOL.GS;
using DOL.GS.Spells;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities.Statics
{
	public class TrapBase : GenericBase 
    {
		protected override string GetStaticName() {return "Rune Of Decimation";}
		protected override ushort GetStaticModel() {return 1;}
		protected override ushort GetStaticEffect() {return 7027;}
		private DBSpell dbs;
		private Spell   s;
		private SpellLine sl;
		int damage;		
		public TrapBase(int damage) 
        {
			
		}
		
		protected override void CastSpell (GameLiving target)
        {
            if (!target.IsAlive) return;
			
		}
	}
}
