using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    [SpellHandler("SnakeCharmer")]
    public class SnakeCharmer : LifedrainSpell
    {
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

        /// <summary>
        /// Uses percent of damage to heal the caster
        /// </summary>
        public override void StealLife(AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * 50 / 100;
            int mana = (ad.Damage + ad.CriticalDamage) * 30 / 100;
            int endu = (ad.Damage + ad.CriticalDamage) * 20 / 100;

            if (m_caster.IsDiseased)
            {
                MessageToCaster("You are diseased!", EChatType.CT_SpellResisted);
                heal >>= 1;
            }
            if (heal <= 0) return;            
            heal = m_caster.ChangeHealth(m_caster, EHealthChangeType.Spell, heal);
            if (heal > 0)
            {
                MessageToCaster("You steal " + heal + " hit point" + (heal == 1 ? "." : "s."), EChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more life.", EChatType.CT_SpellResisted);
            }
            
            if (mana <=0) return;
            mana = m_caster.ChangeMana(m_caster,EPowerChangeType.Spell,mana);
            if (mana > 0)
            {
                MessageToCaster("You steal " + mana + " power point" + (mana == 1 ? "." : "s."), EChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more power.", EChatType.CT_SpellResisted);
            }     
            
            if (endu <=0) return;
            endu = m_caster.ChangeEndurance(m_caster,EEnduranceChangeType.Spell,endu);            
            if (heal > 0)
            {
                MessageToCaster("You steal " + endu + " endurance point" + (endu == 1 ? "." : "s."), EChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more endurance.", EChatType.CT_SpellResisted);
            }
        }

        public SnakeCharmer(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
