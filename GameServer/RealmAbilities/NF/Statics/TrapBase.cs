using Core.Database.Tables;
using Core.GS.Skills;

namespace Core.GS.RealmAbilities;

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