using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Scripts;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.RealmAbilities;

public class NegativeMaelstromBase : GenericBase 
{
	protected override string GetStaticName() {return "Negative Maelstrom";}
	protected override ushort GetStaticModel() => 3738;
	protected override ushort GetStaticEffect() {return 7027;}
	private DbSpell dbs;
	private Spell   s;
	private SpellLine sl;
	int damage;		
	public NegativeMaelstromBase(int damage) 
    {
		this.damage = damage;
		dbs = new DbSpell();
		dbs.Name = GetStaticName();
		dbs.Icon = GetStaticEffect();
		dbs.ClientEffect = GetStaticEffect();
		dbs.Damage = damage;
		dbs.DamageType = (int)EDamageType.Cold;
		dbs.Target = "Enemy";
		dbs.Radius = 0;
        dbs.Type = ESpellType.DirectDamageWithDebuff.ToString();
		dbs.Value = 10;
		dbs.Duration = 10;
		dbs.Pulse = 0;
		dbs.PulsePower = 0;
		dbs.Power = 0;
		dbs.CastTime = 0;
		dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
		sl = new SpellLine("RAs","RealmAbilitys","RealmAbilitys",true);
	}
	protected override void CastSpell (GameLiving target)
    {
        if (!target.IsAlive) return;
		if (GameServer.ServerRules.IsAllowedToAttack(m_caster, target, true))
        {
			int dealDamage =damage;
			if (getCurrentPulse()<=6)
				dealDamage = (int)Math.Round(((double)getCurrentPulse()/6*damage));
			dbs.Damage = dealDamage;				
			s = new Spell(dbs,1);
			ISpellHandler dd = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
			dd.StartSpell(target);
		}
	}
}