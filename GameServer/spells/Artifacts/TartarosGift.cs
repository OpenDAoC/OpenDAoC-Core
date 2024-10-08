using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Tartaros)]
    public class Tartaros : LifedrainSpellHandler
    {
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

        /// <summary>
        /// Uses percent of damage to heal the caster
        /// </summary>
        public override void StealLife(AttackData ad)
        {
            if (ad == null) return;
            if (!Caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * 35 / 100;
            int mana = (ad.Damage + ad.CriticalDamage) * 21 / 100;
            int endu = (ad.Damage + ad.CriticalDamage) * 14 / 100;

            if (Caster.IsDiseased)
            {
                MessageToCaster("You are diseased!", eChatType.CT_SpellResisted);
                heal >>= 1;
            }
            if (heal <= 0) return;            
            heal = Caster.ChangeHealth(Caster, eHealthChangeType.Spell, heal);
            if (heal > 0)
            {
                MessageToCaster("You drain " + heal + " hit point" + (heal == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more life.", eChatType.CT_SpellResisted);
            }
            
            if (mana <=0) return;
            mana = Caster.ChangeMana(Caster,eManaChangeType.Spell,mana);
            if (mana > 0)
            {
                MessageToCaster("You drain " + mana + " power point" + (mana == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more power.", eChatType.CT_SpellResisted);
            }     
            
            if (endu <=0) return;
            endu = Caster.ChangeEndurance(Caster,eEnduranceChangeType.Spell,endu);            
            if (heal > 0)
            {
                MessageToCaster("You drain " + endu + " endurance point" + (endu == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more endurance.", eChatType.CT_SpellResisted);
            }
        }

        public Tartaros(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
