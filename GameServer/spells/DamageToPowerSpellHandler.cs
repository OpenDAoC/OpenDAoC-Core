using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.DamageToPower)]
    public class DamageToPowerSpellHandler : LifedrainSpellHandler
    {
        public override string ShortDescription => $"{Spell.Value}% of any spell damage done to the target is converted to power instead.";

        public DamageToPowerSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Uses percent of damage to power the caster
        /// </summary>
        public override void StealLife(AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
            // Return the spell power? + % calculated on HP value and caster maxmana
            double manareturned = m_spell.Power + (heal * m_caster.MaxMana / 100);

            if (heal <= 0) return;
            heal = m_caster.ChangeMana(m_caster, eManaChangeType.Spell, (int)manareturned);

            if (heal > 0)
            {
                MessageToCaster("You steal " + heal + " power point" + (heal == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more power.", eChatType.CT_SpellResisted);
            }
        }
    }
}
