using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities.Statics 
{
    public class StaticTempestBase : GenericBase  
    {
		protected override string GetStaticName() {return "Static Tempest";}
		protected override ushort GetStaticModel() {return 2654;}
		protected override ushort GetStaticEffect() {return 7032;}
		private DbSpell dbs;
		private Spell   s;
		private SpellLine sl;
		public StaticTempestBase (int stunDuration) 
        {
			dbs = new DbSpell();
			dbs.Name = GetStaticName();
			dbs.Icon = GetStaticEffect();
			dbs.ClientEffect = GetStaticEffect();
			dbs.Damage = 0;
			dbs.DamageType = (int)EDamageType.Energy;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = ESpellType.UnresistableStun.ToString();
			dbs.Value = 0;
			dbs.Duration = stunDuration;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			s = new Spell(dbs,1);
			sl = new SpellLine("RAs","RealmAbilitys","RealmAbilitys",true);
		}
		protected override void CastSpell (GameLiving target) {
            if (!target.IsAlive) return;
			if (GameServer.ServerRules.IsAllowedToAttack(m_caster, target, true))
            {
				ISpellHandler stun = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
				stun.StartSpell(target);
			}
		}
	}
}