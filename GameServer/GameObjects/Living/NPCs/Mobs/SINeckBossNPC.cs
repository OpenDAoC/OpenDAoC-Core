﻿
using DOL.Database;

namespace DOL.GS {
    public class SINeckBoss : GameNPC {
        
        //private static SpellLine sl = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
        //private Spell spell = SkillBase.FindSpell(31250, sl);
        
        public SINeckBoss() : base()
        {
        }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}
		public override int MaxHealth
		{
			get { return (6000 + (Level * 125)); }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}

		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override void OnAttackEnemy(AttackData ad)
        {
           // if(Util.Chance(20))
             //   CastSpell(spell, sl, false);
            if(ad != null && ad.Target != null && ad.Target.IsAlive && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
				if(Util.Chance(20))
					CastSpell(SINeckBossDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
            base.OnAttackEnemy(ad);
        }
		private Spell m_SINeckBossDD;
		public Spell SINeckBossDD
		{
			get
			{
				if (m_SINeckBossDD == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 8;
					spell.ClientEffect = 9644;
					spell.Icon = 9644;
					spell.Damage = 300;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "DD AOE Energy 300";
					spell.Range = 450;
					spell.Radius = 350;
					spell.SpellID = 12000;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_SINeckBossDD = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SINeckBossDD);
				}
				return m_SINeckBossDD;
			}
		}
	}
}
