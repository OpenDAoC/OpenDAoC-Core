using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities.Statics
{
	public class WallOfFlameBase : GenericBase
	{
		protected override string GetStaticName() { return "Wall Of Flame"; }
		protected override ushort GetStaticModel() { return 2651; }
		protected override ushort GetStaticEffect() { return 7050; }
		private DbSpell dbs;
		private Spell s;
		private SpellLine sl;
		public WallOfFlameBase(int damage)
		{
			dbs = new DbSpell();
			dbs.Name = GetStaticName();
			dbs.Icon = GetStaticEffect();
			dbs.ClientEffect = GetStaticEffect();
			dbs.Damage = damage;
			dbs.DamageType = (int)eDamageType.Heat;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = eSpellType.DirectDamage.ToString();
			dbs.Value = 0;
			dbs.Duration = 0;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			s = new Spell(dbs, 1);
			sl = GlobalSpellsLines.RealmSpellsSpellLine;
		}
		protected override void CastSpell(GameLiving target)
		{
			if (!target.IsAlive) return;
			if (GameServer.ServerRules.IsAllowedToAttack(m_caster, target, true))
			{
				ISpellHandler damage = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
				damage.StartSpell(target);
			}
		}
	}
}