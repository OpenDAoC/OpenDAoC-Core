using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Lifedrain)]
    public class LifedrainSpellHandler : DirectDamageSpellHandler
    {
	    
		protected override void DealDamage(GameLiving target)
		{
			if (target == null || !target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// calc damage and healing
			AttackData ad = CalculateDamageToTarget(target);
			// "Your life energy is stolen!"
			MessageToLiving(target, Spell.Message1, eChatType.CT_Spell);
			SendDamageMessages(ad);
			DamageTarget(ad, true);
			StealLife(ad);
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}

        /// <summary>
        /// Uses percent of damage to heal the caster
        /// </summary>
        public virtual void StealLife(AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;

            /*
            if (m_caster.IsDiseased)
            {
                MessageToCaster("You are diseased!", eChatType.CT_SpellResisted);
                heal >>= 1;
            }*/

            if (heal <= 0) return;
            heal = m_caster.ChangeHealth(m_caster, eHealthChangeType.Spell, heal);

            if (heal > 0)
            {
                MessageToCaster("You steal " + heal + " hit point" + (heal == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more life.", eChatType.CT_SpellResisted);
            }
        }

        // constructor
        public LifedrainSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
