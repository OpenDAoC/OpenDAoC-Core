using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities.Statics
{
	public class TrapBase : GenericBase 
    {
		protected override string GetStaticName() {return "Rune Of Decimation";}
		protected override ushort GetStaticModel() {return 1;}
		protected override ushort GetStaticEffect() {return 7027;}
		private DbSpell dbs;
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